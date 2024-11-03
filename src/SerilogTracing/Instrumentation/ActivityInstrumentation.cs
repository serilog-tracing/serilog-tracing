// Copyright © SerilogTracing Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using SerilogTracing.Core;

#if NETSTANDARD2_0
using SerilogTracing.Pollyfill;
#endif

namespace SerilogTracing.Instrumentation;

/// <summary>
/// Utilities for <see cref="IActivityInstrumentor">activity instrumentors</see> to enrich
/// <see cref="Activity">activities</see> for <see cref="Serilog.ILogger">loggers</see>.
/// </summary>
public static class ActivityInstrumentation
{
    internal const string ReplacementActivitySourceName = "SerilogTracing.Instrumentation.ActivityInstrumentation";
    const string DefaultActivityName = "SerilogTracing.Instrumentation.ActivityInstrumentation.Activity";
    const string ReplacedActivityPropertyName = "SerilogTracing.Instrumentation.ActivityInstrumentation.ReplacedActivity";

    static readonly ActivitySource ReplacementActivitySource = new(ReplacementActivitySourceName);
    
    /// <summary>
    /// Associate a <see cref="MessageTemplate"/> with the given <see cref="Activity"/>, without changing the
    /// <see cref="Activity.DisplayName"/>.
    ///
    /// The message template will be assigned to a custom property on the activity, overwriting any previously set value
    /// with the same name.
    /// </summary>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="messageTemplate">The message template to assign.</param>
    public static void SetMessageTemplateOverride(Activity activity, MessageTemplate messageTemplate)
    {
        activity.SetCustomProperty(Constants.MessageTemplateOverridePropertyName, messageTemplate);
    }

    /// <summary>
    /// Get a <see cref="MessageTemplate"/> previously associated with the given <see cref="Activity"/> by
    /// <see cref="ActivityInstrumentation.SetMessageTemplateOverride"/>.
    /// </summary>
    /// <param name="activity">The activity containing the message template.</param>
    /// <param name="messageTemplate">The assigned message template, if any.</param>
    /// <returns>True when the activity contains a message template.</returns>
    internal static bool TryGetMessageTemplateOverride(Activity activity, [NotNullWhen(true)] out MessageTemplate? messageTemplate)
    {
        if (activity.GetCustomProperty(Constants.MessageTemplateOverridePropertyName) is MessageTemplate customPropertyValue)
        {
            messageTemplate = customPropertyValue;
            return true;
        }

        messageTemplate = null;
        return false;
    }

    /// <summary>
    /// Set a property on the given <see cref="Activity"/>, overwriting any previously set value
    /// with the same name.
    /// </summary>
    /// <remarks>
    /// Properties are added to a collection in a custom property on the activity.
    /// If the property value is a <see cref="ScalarValue"/> then it will also set a tag on the activity, making
    /// it visible to outside instrumentation.
    /// </remarks>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="propertyName">The name of the property to assign.</param>
    /// <param name="propertyValue">The value of the property to assign.</param>
    /// <remarks>This override requires fewer allocations than those accepting <see cref="LogEventProperty"/>,</remarks>
    public static void SetLogEventProperty(Activity activity, string propertyName, LogEventPropertyValue propertyValue)
    {
        if (LogEventProperty.IsValidName(propertyName))
        {
            SetPreValidatedLogEventProperty(activity, propertyName, propertyValue, GetOrInitLogEventPropertyCollection(activity));
        }
    }
    
    /// <summary>
    /// Set a <see cref="LogEventProperty"/> on the given <see cref="Activity"/>, overwriting any previously set value
    /// with the same name.
    /// </summary>
    /// <remarks>
    /// Properties are added to a collection in a custom property on the activity.
    /// If the property value is a <see cref="ScalarValue"/> then it will also set a tag on the activity, making
    /// it visible to outside instrumentation.
    /// </remarks>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="property">The property to assign.</param>
    public static void SetLogEventProperty(Activity activity, LogEventProperty property)
    {
        SetPreValidatedLogEventProperty(activity, property.Name, property.Value, GetOrInitLogEventPropertyCollection(activity));
    }

    /// <summary>
    /// Set multiple <see cref="LogEventProperty">log event properties</see>, overwriting any previously set values
    /// with the same names.
    /// </summary>
    /// <remarks>
    /// This method behaves like multiple calls to <see cref="ActivityInstrumentation.SetLogEventProperty(Activity, LogEventProperty)"/>, but
    /// avoids additional dictionary lookups once the first property is added.
    /// </remarks>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="properties">The properties to assign.</param>
    public static void SetLogEventProperties(Activity activity, params LogEventProperty[] properties)
    {
        var collection = GetOrInitLogEventPropertyCollection(activity);

        foreach (var property in properties)
        {
            SetPreValidatedLogEventProperty(activity, property.Name, property.Value, collection);
        }
    } 
    
    /// <summary>
    /// Set multiple <see cref="LogEventProperty">log event properties</see>, overwriting any previously set values
    /// with the same names.
    /// </summary>
    /// <remarks>
    /// This method behaves like multiple calls to <see cref="ActivityInstrumentation.SetLogEventProperty(Activity, LogEventProperty)"/>, but
    /// avoids additional dictionary lookups once the first property is added.
    /// </remarks>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="properties">The properties to assign.</param>
    public static void SetLogEventProperties(Activity activity, IEnumerable<LogEventProperty> properties)
    {
        var collection = GetOrInitLogEventPropertyCollection(activity);

        foreach (var property in properties)
        {
            SetPreValidatedLogEventProperty(activity, property.Name, property.Value, collection);
        }
    }

    internal static void SetPreValidatedLogEventProperty(Activity activity, string propertyName, LogEventPropertyValue propertyValue, Dictionary<string, LogEventPropertyValue> collection)
    {
        activity.SetTag(propertyName, ToActivityTagValue(propertyValue));
        collection[propertyName] = propertyValue;
    }

    static object? ToActivityTagValue(LogEventPropertyValue propertyValue)
    {
        return propertyValue is ScalarValue sv ? sv.Value : propertyValue;
    }

    internal static bool TryGetLogEventPropertyCollection(Activity activity, [NotNullWhen(true)] out Dictionary<string, LogEventPropertyValue>? properties)
    {
        if (activity.GetCustomProperty(Constants.LogEventPropertyCollectionName) is Dictionary<string, LogEventPropertyValue> existing)
        {
            properties = existing;
            return true;
        }

        properties = null;
        return false;
    }

    static Dictionary<string, LogEventPropertyValue> GetOrInitLogEventPropertyCollection(Activity activity)
    {
        if (TryGetLogEventPropertyCollection(activity, out var existing))
        {
            return existing;
        }

        var added = new Dictionary<string, LogEventPropertyValue>();
        activity.SetCustomProperty(Constants.LogEventPropertyCollectionName, added);

        return added;
    }

    /// <summary>
    /// Try associate an <see cref="Exception"/> with the activity.
    ///
    /// The exception will be stored as an event with the current timestamp, using the conventional
    /// name "exception".
    ///
    /// This method won't overwrite any previously set exception.
    /// </summary>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="exception">True if the exception was set on the event.</param>
    public static bool TrySetException(Activity activity, Exception exception)
    {
        if (activity.Events.Any(e => e.Name == Constants.ExceptionEventName)) return false;

        activity.AddEvent(EventFromException(exception));
        return true;
    }

    static ActivityEvent EventFromException(Exception exception)
    {
        var tags = new ActivityTagsCollection
        {
            ["exception.stacktrace"] = exception.ToString(),
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message
        };
        return new ActivityEvent(Constants.ExceptionEventName, DateTimeOffset.Now, tags);
    }

    /// <summary>
    /// Try get an exception previously associated with the activity using the conventional event name "exception".
    /// </summary>
    /// <param name="activity">The activity containing the exception.</param>
    /// <param name="exception">True if an exception event is present on the activity. The type of the returned
    /// exception is not guaranteed to match the one originally set on the activity.</param>
    /// <returns></returns>
    internal static bool TryGetException(Activity activity, [NotNullWhen(true)] out Exception? exception)
    {
        exception = ExceptionFromEvents(activity);

        return exception != null;
    }

    internal static bool IsException(ActivityEvent activityEvent)
    {
        return activityEvent.Name == Constants.ExceptionEventName;
    }

    static Exception? ExceptionFromEvents(Activity activity)
    {
        var first = activity.Events.FirstOrDefault(IsException);
        if (first.Name == default(ActivityEvent).Name)
            return null;

        return ExceptionFromEvent(first);
    }

    internal static Exception ExceptionFromEvent(ActivityEvent activityEvent)
    {
        return new TextException(
            activityEvent.Tags.FirstOrDefault(t => t.Key == Constants.ExceptionMessageTagName).Value as string,
            activityEvent.Tags.FirstOrDefault(t => t.Key == Constants.ExceptionTypeTagName).Value as string,
            activityEvent.Tags.FirstOrDefault(t => t.Key == Constants.ExceptionStackTraceTagName).Value as string);
    }

    class TextException(
        string? message,
        string? type,
        string? toString) : Exception(message ?? type)
    {
        public override string ToString() => toString ?? "No information available.";
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
    public static void StartReplacementActivity(
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

            // This method should be called in an `ActivityStarted` callback
            // so the replaced activity should already be started. We need
            // to start the replacement activity here
            replacement.Start();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static void StopReplacementActivity()
    {
        var replacement = Activity.Current;
        
        if (replacement?.GetCustomProperty(ReplacedActivityPropertyName) is Activity replaced)
        {
            // This method should be called in an `ActivityStopped` callback
            // so the replacement activity should already be stopped. We
            // need to stop the replaced activity here
            replaced.Stop();

            Activity.Current = replaced;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="replacedActivity"></param>
    /// <returns></returns>
    public static bool TryGetReplacedActivity(Activity activity, [NotNullWhen(true)] out Activity? replacedActivity)
    {
        if (activity.GetCustomProperty(ReplacedActivityPropertyName) is Activity original)
        {
            replacedActivity = original;
            return true;
        }

        replacedActivity = null;
        return false;
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
    internal static Activity? CreateReplacementActivity(
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
            replace!.ParentId != null && TryParseTraceParentHeader(replace.ParentId, out var parsed))
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
        
        var replacement = ReplacementActivitySource.CreateActivity(DefaultActivityName, replace?.Kind ?? ActivityKind.Internal, context);

        if (replace == null)
        {
            return replacement;
        }

        if (replacement != null)
        {
            replacement.SetCustomProperty(ReplacedActivityPropertyName, replace);

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
    
    internal static bool TryParseTraceParentHeader(string traceParentHeaderValue, [NotNullWhen(true)] out ActivityTraceFlags? flags)
    {
        if (traceParentHeaderValue.EndsWith("-00"))
        {
            flags = ActivityTraceFlags.None;
            return true;
        }
        
        if (traceParentHeaderValue.EndsWith("-01"))
        {
            flags = ActivityTraceFlags.Recorded;
            return true;
        }

        flags = null;
        return false;
    }

    internal static void AttachLoggerActivity(Activity activity, LoggerActivity loggerActivity)
    {
        activity.SetCustomProperty(Constants.SelfPropertyName, loggerActivity);
        activity.SetCustomProperty(Constants.LogEventPropertyCollectionName, loggerActivity.Properties);
        foreach (var (name, value) in loggerActivity.Properties)
        {
            activity.AddTag(name, ToActivityTagValue(value));
        }
    }

    internal static bool HasAttachedLoggerActivity(Activity activity)
    {
        return activity.GetCustomProperty(Constants.SelfPropertyName) is LoggerActivity;
    }

    internal static bool IsSuppressed(Activity? activity)
    {
        return activity is not { Recorded: true };
    }

    internal static bool IsDataSuppressed(Activity? activity)
    {
        return activity is not { IsAllDataRequested: true, Recorded: true };
    }
}
