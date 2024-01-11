using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using static SerilogTracing.Core.Constants;

namespace SerilogTracing.Interop;

static class ActivityUtil
{
    internal static void SetLoggerActivity(Activity activity, LoggerActivity loggerActivity)
    {
        activity.SetCustomProperty(SelfPropertyName, loggerActivity);
    }
    
    internal static bool TryGetLoggerActivity(Activity activity, [NotNullWhen(true)] out LoggerActivity? loggerActivity)
    {
        if (activity.GetCustomProperty(SelfPropertyName) is LoggerActivity customPropertyValue)
        {
            loggerActivity = customPropertyValue;
            return true;
        }

        loggerActivity = null;
        return false;
    }
    
    internal static LogEvent ActivityToLogEvent(ILogger logger, Activity activity)
    {
        var start = activity.StartTimeUtc;
        var end = start + activity.Duration;
        var level = activity.Status == ActivityStatusCode.Error ? LogEventLevel.Error : LogEventLevel.Information;
        activity.TryGetMessageTemplateOverride(out var messageTemplate);
        var template = messageTemplate ?? new MessageTemplate(new[] { new TextToken(activity.DisplayName) });
        var exception = ExceptionFromEvents(activity);
        var properties = activity.GetLogEventProperties().ToDictionary(p => p.Name);
        return ActivityToLogEvent(logger, activity, start, end, activity.TraceId, activity.SpanId, activity.ParentSpanId, level, exception, template, properties);
    }
    
    internal static LogEvent ActivityToLogEvent(ILogger logger, LoggerActivity loggerActivity)
    {
        var start = loggerActivity.StartTimestamp;
        var end = start + loggerActivity.Duration;
        var traceId = loggerActivity.TraceId;
        var spanId = loggerActivity.SpanId;
        var parentSpanId = loggerActivity.ParentSpanId;
        var level = loggerActivity.CompletionLevel ?? (loggerActivity.Activity?.GetCompletionLevel() ?? LogEventLevel.Information);
        var template = loggerActivity.MessageTemplate;
        var exception = loggerActivity.Exception ?? (loggerActivity.Activity != null ? ExceptionFromEvents(loggerActivity.Activity) : null);
        var properties = loggerActivity.Captures.Concat(loggerActivity.Activity?.GetLogEventProperties() ?? Enumerable.Empty<LogEventProperty>()).ToDictionary(p => p.Name);
        return ActivityToLogEvent(logger, loggerActivity.Activity, start, end, traceId, spanId, parentSpanId, level, exception, template, properties);
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
    
    internal static ActivityEvent EventFromException(Exception exception)
    {
        var tags = new ActivityTagsCollection
        {
            ["exception.stacktrace"] = exception.ToString(),
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message
        };
        return new ActivityEvent(ExceptionEventName, DateTimeOffset.Now, tags);
    }

    internal static Exception? ExceptionFromEvents(Activity activity)
    {
        var first = activity.Events.FirstOrDefault(e => e.Name == "exception");
        if (first.Name != "exception")
            return null;
        return new TextException(
            first.Tags.FirstOrDefault(t => t.Key == "exception.message").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.type").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value as string);
    }
    
    internal static bool TryGetLogEventPropertyCollection(Activity activity, [NotNullWhen(true)] out Dictionary<string, LogEventProperty>? properties)
    {
        if (activity.GetCustomProperty(LogEventPropertyCollectionName) is Dictionary<string, LogEventProperty> existing)
        {
            properties = existing;
            return true;
        }

        properties = null;
        return false;
    }

    internal static Dictionary<string, LogEventProperty> GetOrInitLogEventPropertyCollection(Activity activity)
    {
        if (TryGetLogEventPropertyCollection(activity, out var existing))
        {
            return existing;
        }

        var added = new Dictionary<string, LogEventProperty>();
        activity.SetCustomProperty(LogEventPropertyCollectionName, added);

        return added;
    }
}