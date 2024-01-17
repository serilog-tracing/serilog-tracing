using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Instrumentation;
using static SerilogTracing.Core.Constants;

namespace SerilogTracing.Interop;

static class ActivityConvert
{
    internal static LogEvent ActivityToLogEvent(ILogger logger, Activity activity)
    {
        var start = activity.StartTimeUtc;
        var end = start + activity.Duration;
        var level = activity.Status == ActivityStatusCode.Error ? LogEventLevel.Error : LogEventLevel.Information;
        ActivityInstrumentation.TryGetMessageTemplateOverride(activity, out var messageTemplate);
        var template = messageTemplate ?? new MessageTemplate(new[] { new TextToken(activity.DisplayName) });
        ActivityInstrumentation.TryGetException(activity, out var exception);
        var properties = ActivityInstrumentation.GetLogEventProperties(activity).ToDictionary(p => p.Name);
        return ActivityToLogEvent(logger, activity, start, end, activity.TraceId, activity.SpanId, activity.ParentSpanId, level, exception, template, properties);
    }
    
    internal static LogEvent ActivityToLogEvent(ILogger logger, LoggerActivity loggerActivity)
    {
        var start = loggerActivity.StartTimestamp;
        var end = start + loggerActivity.Duration;
        var traceId = loggerActivity.TraceId;
        var spanId = loggerActivity.SpanId;
        var parentSpanId = loggerActivity.ParentSpanId;
        var level = loggerActivity.CompletionLevel ?? ((loggerActivity.Activity != null
            ? ActivityInstrumentation.GetCompletionLevel(loggerActivity.Activity) as LogEventLevel?
            : null) ?? LogEventLevel.Information);
        var template = loggerActivity.MessageTemplate;
        var exception = loggerActivity.Exception;
        if (exception == null && loggerActivity.Activity != null)
        {
            ActivityInstrumentation.TryGetException(loggerActivity.Activity, out exception);
        }
        return ActivityToLogEvent(logger, loggerActivity.Activity, start, end, traceId, spanId, parentSpanId, level, exception, template, loggerActivity.Properties);
    }

    internal static LogEvent ActivityToLogEvent(
        ILogger logger,
        Activity? activity,
        DateTime start,
        DateTime end,
        ActivityTraceId? traceId,
        ActivitySpanId? spanId,
        ActivitySpanId? parentSpanId,
        LogEventLevel level,
        Exception? exception,
        MessageTemplate messageTemplate,
        Dictionary<string, LogEventProperty> properties)
    {
        if (activity is { } a)
        {
            foreach (var tag in a.Tags.Concat(a.Baggage).Select(t => new KeyValuePair<string, object?>(t.Key, t.Value)).Concat(a.TagObjects))
            {
                if (properties.ContainsKey(tag.Key))
                    continue;

                if (!logger.BindProperty(tag.Key, tag.Value, destructureObjects: false, out var property))
                    continue;

                properties.Add(tag.Key, property);
            }
        }

        properties[SpanStartTimestampPropertyName] = new LogEventProperty(SpanStartTimestampPropertyName, new ScalarValue(start));
        if (parentSpanId != null && parentSpanId.Value != default)
        {
            properties[ParentSpanIdPropertyName] = new LogEventProperty(ParentSpanIdPropertyName, new ScalarValue(parentSpanId.Value));
        }

        var evt = new LogEvent(
            end,
            level,
            exception,
            messageTemplate,
            properties.Values,
            traceId ?? default,
            spanId ?? default);

        return evt;
    }
}