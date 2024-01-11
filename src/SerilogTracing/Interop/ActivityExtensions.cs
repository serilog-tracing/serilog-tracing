using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using SerilogTracing.Core;

namespace SerilogTracing.Interop;

/// <summary>
/// 
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="messageTemplate"></param>
    public static void SetMessageTemplateOverride(this Activity activity, MessageTemplate messageTemplate)
    {
        activity.SetCustomProperty(Constants.MessageTemplateOverridePropertyName, messageTemplate);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="messageTemplate"></param>
    /// <returns></returns>
    public static bool TryGetMessageTemplateOverride(this Activity activity, [NotNullWhen(true)] out MessageTemplate? messageTemplate)
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
    public static void SetLogEventProperty(this Activity activity, LogEventProperty property)
    {
        activity.SetLogEventProperties(Enumerable.Repeat(property, 1));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="properties"></param>
    public static void SetLogEventProperties(this Activity activity, IEnumerable<LogEventProperty> properties)
    {
        var collection = ActivityUtil.GetOrInitLogEventPropertyCollection(activity);

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
    public static IEnumerable<LogEventProperty> GetLogEventProperties(this Activity activity)
    {
        return ActivityUtil.TryGetLogEventPropertyCollection(activity, out var existing) ? existing.Values : Enumerable.Empty<LogEventProperty>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="exception"></param>
    public static bool TrySetException(this Activity activity, Exception exception)
    {
        if (activity.Events.Any(e => e.Name == Constants.ExceptionEventName)) return false;
        
        activity.AddEvent(ActivityUtil.EventFromException(exception));
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static bool TryGetException(this Activity activity, [NotNullWhen(true)] out Exception? exception)
    {
        exception = ActivityUtil.ExceptionFromEvents(activity);

        return exception != null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static LogEventLevel GetCompletionLevel(this Activity activity)
    {
        return activity.Status == ActivityStatusCode.Error ? LogEventLevel.Error : LogEventLevel.Information;
    }
}