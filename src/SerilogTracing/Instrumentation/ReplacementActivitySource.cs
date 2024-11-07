using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SerilogTracing.Core;

#if NETSTANDARD2_0
using SerilogTracing.Pollyfill;
#endif

namespace SerilogTracing.Instrumentation;

/// <summary>
/// 
/// </summary>
public class ReplacementActivitySource : IDisposable
{
    const string DefaultActivityName = "SerilogTracing.Instrumentation.ActivityInstrumentation.Activity";
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public ReplacementActivitySource(string name)
    {
        _source = new ActivitySource(name);

        _listener = new ActivityListener();
        _listener.ShouldListenTo = source => source.Name == name;
        
        _listener.Sample = (ref ActivityCreationOptions<ActivityContext> options) => options.Source == _source
            ? ActivitySamplingResult.AllDataAndRecorded
            : ActivitySamplingResult.None;
        
        _listener.ActivityStopped += activity =>
        {
            if (TryGetReplacementActivity(activity, out var replacement))
            {
                replacement.Stop();
            }
        };
        
        ActivitySource.AddActivityListener(_listener);
    }

    readonly ActivitySource _source;
    readonly ActivityListener _listener;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public bool CanReplace(ActivitySource source)
    {
        return source != _source && source.Name == _source.Name;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configureReplacement"></param>
    /// <param name="postSamplingFilter"></param>
    /// <param name="inheritTags"></param>
    /// <param name="inheritParent"></param>
    /// <param name="inheritFlags"></param>
    /// <param name="inheritBaggage"></param>
    /// <returns></returns>
    public void StartReplacementActivity(
        Func<Activity?, bool> postSamplingFilter,
        Action<Activity> configureReplacement,
        bool inheritTags = true,
        bool inheritParent = true,
        bool inheritFlags = true,
        bool inheritBaggage = true
    ) {
        var replace = Activity.Current;
        
        // Important to do this first, otherwise our activity source will consult the inherited
        // activity when making sampling decisions.
        Activity.Current = replace?.Parent;

        var replacement = CreateReplacementActivity(replace, inheritTags, inheritParent, inheritFlags, inheritBaggage);

        if (replace != null)
        {
            // Suppress the original activity
            replace.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            replace.IsAllDataRequested = false;
        }

        if (replacement != null)
        {
            if (!postSamplingFilter(replacement))
            {
                // The post-sampling filter can unilaterally suppress activities.
                replacement.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
            else if (replacement.Recorded)
            {
                configureReplacement(replacement);
            }

            replacement.Start();
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="replace"></param>
    /// <param name="inheritTags"></param>
    /// <param name="inheritParent"></param>
    /// <param name="inheritFlags"></param>
    /// <param name="inheritBaggage"></param>
    /// <returns></returns>
    internal Activity? CreateReplacementActivity(
        Activity? replace,
        bool inheritTags,
        bool inheritParent,
        bool inheritFlags,
        bool inheritBaggage
    ) {
        // We're only interested in the incoming parent if there is one. Switching off `inheritParent` when there isn't,
        // prevents us from trying to override a nonexistent sampling decision a little further down. Checking
        // `HasRemoteParent` would be useful here, but it creates problems for unit testing.
        inheritParent = inheritParent && replace != null &&
                        replace.ParentSpanId.ToHexString() != default(ActivitySpanId).ToHexString();

        var flags = ActivityTraceFlags.None;
        if (inheritParent && inheritFlags &&
            replace!.ParentId != null && TraceParentHeader.TryParse(replace.ParentId, out var parsed))
        {
            flags = parsed.Value;
        }

        var context = inheritParent && inheritFlags ?
            new ActivityContext(
                replace!.TraceId,
                replace.ParentSpanId,
                flags,
                isRemote: true) :
            default;
        
        var replacement = _source.CreateActivity(DefaultActivityName, replace?.Kind ?? ActivityKind.Internal, context);

        if (replace == null)
        {
            return replacement;
        }

        if (replacement != null)
        {
            replacement.SetCustomProperty(Constants.ReplacedActivityPropertyName, replace);
            replace.SetCustomProperty(Constants.ReplacementActivityPropertyName, replacement);

            if (inheritTags)
            {
#if FEATURE_ACTIVITY_ENUMERATETAGOBJECTS
                foreach (var (name, value) in incoming.EnumerateTagObjects())
#else
                foreach (var (name, value) in replace.TagObjects)
#endif
                {
                    replacement.SetTag(name, value);
                }
            }

            if (inheritParent)
            {
                if (inheritFlags)
                {
                    // In `Trust` mode we override the local sampling decision with the remote one. We
                    // already used the incoming trace and parent span ids through the `context` passed
                    // to `CreateActivity`.
                    replacement.ActivityTraceFlags = flags;
                }
                else
                {
                    replacement.SetParentId(replace.TraceId, replace.ParentSpanId, replacement.ActivityTraceFlags);
                }
            }

            if (inheritBaggage)
            {
                foreach (var (k, v) in replace.Baggage)
                {
                    replacement.SetBaggage(k, v);
                }
            }
        }
        
        return replacement;
    }
    
    static bool TryGetReplacementActivity(Activity activity, [NotNullWhen(true)] out Activity? replacementActivity)
    {
        if (activity.GetCustomProperty(Constants.ReplacementActivityPropertyName) is Activity original)
        {
            replacementActivity = original;
            return true;
        }

        replacementActivity = null;
        return false;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _source.Dispose();
        _listener.Dispose();
    }
}