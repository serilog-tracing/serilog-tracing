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
    /// <returns></returns>
    public static LogEventLevel GetCompletionLevel(this Activity activity)
    {
        return activity.Status == ActivityStatusCode.Error ? LogEventLevel.Error : LogEventLevel.Information;
    }
}