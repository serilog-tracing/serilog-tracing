using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// 
/// </summary>
public static class ActivityInstrumentation
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="messageTemplate"></param>
    public static void SetMessageTemplateOverride(Activity activity, MessageTemplate messageTemplate)
    {
        activity.SetCustomProperty(Constants.MessageTemplateOverridePropertyName, messageTemplate);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="messageTemplate"></param>
    /// <returns></returns>
    public static bool TryGetMessageTemplateOverride(Activity activity, [NotNullWhen(true)] out MessageTemplate? messageTemplate)
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
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="property"></param>
    public static void SetLogEventProperty(Activity activity, LogEventProperty property)
    {
        SetLogEventProperties(activity, Enumerable.Repeat(property, 1));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="properties"></param>
    public static void SetLogEventProperties(Activity activity, IEnumerable<LogEventProperty> properties)
    {
        var collection = GetOrInitLogEventPropertyCollection(activity);

        foreach (var property in properties)
        {
            if (property.Value is ScalarValue sv)
            {
                activity.SetTag(property.Name, sv.Value);
            }
        
            collection.Add(property.Name, property);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static IEnumerable<LogEventProperty> GetLogEventProperties(Activity activity)
    {
        return TryGetLogEventPropertyCollection(activity, out var existing) ? existing.Values : Enumerable.Empty<LogEventProperty>();
    }
    
    static bool TryGetLogEventPropertyCollection(Activity activity, [NotNullWhen(true)] out Dictionary<string, LogEventProperty>? properties)
    {
        if (activity.GetCustomProperty(Constants.LogEventPropertyCollectionName) is Dictionary<string, LogEventProperty> existing)
        {
            properties = existing;
            return true;
        }

        properties = null;
        return false;
    }

    static Dictionary<string, LogEventProperty> GetOrInitLogEventPropertyCollection(Activity activity)
    {
        if (TryGetLogEventPropertyCollection(activity, out var existing))
        {
            return existing;
        }

        var added = new Dictionary<string, LogEventProperty>();
        activity.SetCustomProperty(Constants.LogEventPropertyCollectionName, added);

        return added;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="exception"></param>
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
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static bool TryGetException(Activity activity, [NotNullWhen(true)] out Exception? exception)
    {
        exception = ExceptionFromEvents(activity);

        return exception != null;
    }
    
    static Exception? ExceptionFromEvents(Activity activity)
    {
        var first = activity.Events.FirstOrDefault(e => e.Name == "exception");
        if (first.Name != "exception")
            return null;
        
        return new TextException(
            first.Tags.FirstOrDefault(t => t.Key == "exception.message").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.type").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value as string);
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
    /// <param name="activity"></param>
    /// <returns></returns>
    public static LogEventLevel GetCompletionLevel(Activity activity)
    {
        return activity.Status == ActivityStatusCode.Error ? LogEventLevel.Error : LogEventLevel.Information;
    }
}