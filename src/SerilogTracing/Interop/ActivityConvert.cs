using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Instrumentation;
using static SerilogTracing.Core.Constants;

namespace SerilogTracing.Interop;

static class ActivityConvert
{
    internal static LogEvent ActivityToLogEvent(ILogger logger, Activity activity, LogEventLevel level)
    {
        var start = activity.StartTimeUtc;
        
        // Note that races around time zone changes may cause local-time display issues, but the instant will
        // be recorded correctly and this is what machine-readable outputs will see.
        var end = new DateTimeOffset(start + activity.Duration).ToLocalTime();
        
        ActivityInstrumentation.TryGetMessageTemplateOverride(activity, out var messageTemplate);
        var template = messageTemplate ?? new MessageTemplate(new[] { new TextToken(activity.DisplayName) });
        ActivityInstrumentation.TryGetException(activity, out var exception);
        var properties = ActivityInstrumentation.TryGetLogEventPropertyCollection(activity, out var activityProperties)
            ? activityProperties
            : new Dictionary<string, LogEventPropertyValue>();
        
        return ActivityToLogEvent(logger, activity, start, end, activity.TraceId, activity.SpanId, activity.ParentSpanId, level, exception, template, properties);
    }
    
    internal static LogEvent ActivityToLogEvent(ILogger logger, LoggerActivity loggerActivity, DateTimeOffset end, LogEventLevel level, Exception? exception)
    {
        var activity = loggerActivity.Activity!;

        var start = activity.StartTimeUtc;
        var traceId = activity.TraceId;
        var spanId = activity.SpanId;
        var parentSpanId = activity.ParentSpanId;
        var template = loggerActivity.MessageTemplate;
        if (exception == null)
        {
            ActivityInstrumentation.TryGetException(activity, out exception);
        }
        return ActivityToLogEvent(logger, loggerActivity.Activity, start, end, traceId, spanId, parentSpanId, level, exception, template, loggerActivity.Properties);
    }

    internal static LogEvent ActivityToLogEvent(
        ILogger logger,
        Activity? activity,
        DateTime start,
        DateTimeOffset end,
        ActivityTraceId? traceId,
        ActivitySpanId? spanId,
        ActivitySpanId? parentSpanId,
        LogEventLevel level,
        Exception? exception,
        MessageTemplate messageTemplate,
        Dictionary<string, LogEventPropertyValue> properties)
    {
        if (activity != null)
        {
#if FEATURE_ACTIVITY_STRUCTENUMERATORS
            foreach (var tag in activity.EnumerateTagObjects())
#else
            foreach (var tag in activity.TagObjects)
#endif
            {
                if (properties.ContainsKey(tag.Key))
                    continue;

                if (!logger.BindProperty(tag.Key, tag.Value, destructureObjects: false, out var property))
                    continue;

                properties.Add(tag.Key, property.Value);
            }
        }

        properties[SpanStartTimestampPropertyName] = new ScalarValue(start);
        if (parentSpanId != null && parentSpanId.Value != default)
        {
            properties[ParentSpanIdPropertyName] = new ScalarValue(parentSpanId.Value);
        }

        var evt = new LogEvent(
            end,
            level,
            exception,
            messageTemplate,
            Enumerable.Empty<LogEventProperty>(),
            traceId ?? default,
            spanId ?? default);

        // NOTE: This is a temporary approach to assembling the `LogEvent` to try avoid
        // some allocations. Ideally we'll have an (unsafe) way to build a `LogEvent`
        // directly from the pre-built `Dictionary<string, LogEventPropertyValue>`.
        // In the meantime this is still _slightly_ better than storing a `Dictionary<string, LogEventProperty>`.
        //
        // The values in this dictionary were originally constructed and validated through `LogEventProperty`,
        // so there's no potential for any keys to be invalid property names.
        if (evt.Properties is Dictionary<string, LogEventPropertyValue> evtProperties)
        {
            foreach (var kv in properties)
            {
                evtProperties.Add(kv.Key, kv.Value);
            }
        }
        // We never actually expect to hit this branch, but it's here just to be safe
        else
        {
            foreach (var kv in properties)
            {
                evt.AddOrUpdateProperty(new(kv.Key, kv.Value));
            }
        }

        return evt;
    }
}