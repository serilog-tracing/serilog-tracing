using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SerilogTracing.Core;
using SerilogTracing.Interop;

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
        
        // The listener always wants activities from its own internal source
        // For any other source (including the one it's potentially replacing activities from)
        // it doesn't contribute any sampling decision
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
        return source != _source;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="postSamplingFilter"></param>
    /// <param name="configureReplacement"></param>
    /// <param name="parentOptions"></param>
    public void StartReplacementActivity(
        Func<Activity?, bool> postSamplingFilter,
        Action<Activity> configureReplacement,
        ReplacementActivityParentOptions? parentOptions = null
    ) {
        var replace = Activity.Current;
        
        // Important to do this first, otherwise our activity source will consult the inherited
        // activity when making sampling decisions.
        Activity.Current = replace?.Parent;

        var replacement = CreateReplacementActivity(replace, parentOptions ?? ReplacementActivityParentOptions.InheritAll);

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
    
     internal Activity? CreateReplacementActivity(
        Activity? replace,
        ReplacementActivityParentOptions parentOptions
    ) {
        // We're only interested in the incoming parent if there is one. Switching off `inheritParent` when there isn't,
        // prevents us from trying to override a nonexistent sampling decision a little further down. Checking
        // `HasRemoteParent` would be useful here, but it creates problems for unit testing.
        parentOptions.InheritParent = parentOptions.InheritParent && replace != null &&
                                replace.ParentSpanId.ToHexString() != default(ActivitySpanId).ToHexString();

        var flags = ActivityTraceFlags.None;
        if (parentOptions.InheritParent && parentOptions.InheritFlags &&
            replace!.ParentId != null && TraceParentHeader.TryParse(replace.ParentId, out var parsed))
        {
            flags = parsed.Value;
        }

        var context = parentOptions.InheritParent && parentOptions.InheritFlags ?
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

            if (parentOptions.InheritTags)
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

            if (parentOptions.InheritParent)
            {
                if (parentOptions.InheritFlags)
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

            if (parentOptions.InheritBaggage)
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