using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Interop;

// ReSharper disable MemberCanBePrivate.Global

namespace SerilogTracing;

/// <summary>
/// Extends <see cref="ILogger"/> with methods for creating activities that are emitted to the logger
/// as spans.
/// </summary>
public static class LoggerTracingExtensions
{
    const string FallbackTemplateOriginalTemplateName = "InvalidTemplate";
    static readonly MessageTemplate FallbackTemplate = new MessageTemplateParser().Parse($"{{{FallbackTemplateOriginalTemplateName}}}");

    // This method checks preconditions for starting an activity, before a params array or `LoggerActivity` might need to be allocated.
    static bool TryStartActivity(ILogger logger, LogEventLevel level, string messageTemplate, [NotNullWhen(true)] out Activity? activity)
    {
        if (logger == null! || messageTemplate == null!)
        {
            activity = null;
            return false;
        }
        
        if (!logger.IsEnabled(level))
        {
            activity = null;
            return false;
        }

        activity = LoggerActivitySource.TryStartActivity(messageTemplate);
        return activity != null;
    }
   
    // This method performs all of the allocations on behalf of the new activity and is intended to be infallible, since the returned
    // object manages the lifetime of the already-started `Activity`.
    static LoggerActivity BindLoggerActivity(ILogger logger, LogEventLevel level, string messageTemplate, object?[]? propertyValues, Activity activity)
    {
        if (!logger.BindMessageTemplate(messageTemplate, propertyValues, out var parsedTemplate, out var captures))
        {
            parsedTemplate = FallbackTemplate;
            captures = new[] { new LogEventProperty(FallbackTemplateOriginalTemplateName, new ScalarValue(messageTemplate)) };
        }

        return new LoggerActivity(logger, level, activity, parsedTemplate, captures);
    }
    
    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="level">The <see cref="LogEventLevel"/> of the <see cref="LogEvent"/> generated when the activity
    /// is completed. The <see cref="LoggerActivity.Complete"/> method can be used to override this with
    /// a higher level, but the level cannot be lowered at completion.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValues">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity(this ILogger logger, LogEventLevel level, string messageTemplate, params object?[]? propertyValues)
    {
        return !TryStartActivity(logger, level, messageTemplate, out var activity) ?
            LoggerActivity.None :
            BindLoggerActivity(logger, level, messageTemplate, propertyValues, activity);
    }
    
    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValues">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity(this ILogger logger, string messageTemplate, params object?[]? propertyValues)
        => StartActivity(logger, LogEventLevel.Information, messageTemplate, propertyValues);
    
    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="level">The <see cref="LogEventLevel"/> of the <see cref="LogEvent"/> generated when the activity
    /// is completed. The <see cref="LoggerActivity.Complete"/> method can be used to override this with
    /// a higher level, but the level cannot be lowered at completion.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity(this ILogger logger, LogEventLevel level, string messageTemplate) 
    {
        return !TryStartActivity(logger, level, messageTemplate, out var activity) ?
            LoggerActivity.None :
            BindLoggerActivity(logger, level, messageTemplate, [], activity);
    }
    
    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity(this ILogger logger, string messageTemplate) 
        => StartActivity(logger, LogEventLevel.Information, messageTemplate);

    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="level">The <see cref="LogEventLevel"/> of the <see cref="LogEvent"/> generated when the activity
    /// is completed. The <see cref="LoggerActivity.Complete"/> method can be used to override this with
    /// a higher level, but the level cannot be lowered at completion.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValue">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<T>(this ILogger logger, LogEventLevel level, string messageTemplate, T propertyValue) 
    {
        return !TryStartActivity(logger, level, messageTemplate, out var activity) ?
            LoggerActivity.None :
            BindLoggerActivity(logger, level, messageTemplate, [propertyValue], activity);
    }
    
    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValue">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<T>(this ILogger logger, string messageTemplate, T propertyValue) 
        => StartActivity(logger, LogEventLevel.Information, messageTemplate, propertyValue);

    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="level">The <see cref="LogEventLevel"/> of the <see cref="LogEvent"/> generated when the activity
    /// is completed. The <see cref="LoggerActivity.Complete"/> method can be used to override this with
    /// a higher level, but the level cannot be lowered at completion.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValue0">Values to substitute into the <paramref name="messageTemplate"/> placeholders.</param>
    /// <param name="propertyValue1">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<T0, T1>(this ILogger logger, LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        return !TryStartActivity(logger, level, messageTemplate, out var activity) ?
            LoggerActivity.None :
            BindLoggerActivity(logger, level, messageTemplate, [propertyValue0, propertyValue1], activity);
    }
    
    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValue0">Values to substitute into the <paramref name="messageTemplate"/> placeholders.</param>
    /// <param name="propertyValue1">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<T0, T1>(this ILogger logger, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
        => StartActivity(logger, LogEventLevel.Information, messageTemplate, propertyValue0, propertyValue1);

    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="level">The <see cref="LogEventLevel"/> of the <see cref="LogEvent"/> generated when the activity
    /// is completed. The <see cref="LoggerActivity.Complete"/> method can be used to override this with
    /// a higher level, but the level cannot be lowered at completion.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValue0">Values to substitute into the <paramref name="messageTemplate"/> placeholders.</param>
    /// <param name="propertyValue1">Values to substitute into the <paramref name="messageTemplate"/> placeholders.</param>
    /// <param name="propertyValue2">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<T0, T1, T2>(this ILogger logger, LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        return !TryStartActivity(logger, level, messageTemplate, out var activity) ?
            LoggerActivity.None :
            BindLoggerActivity(logger, level, messageTemplate, [propertyValue0, propertyValue1, propertyValue2], activity);
    }
    
    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValue0">Values to substitute into the <paramref name="messageTemplate"/> placeholders.</param>
    /// <param name="propertyValue1">Values to substitute into the <paramref name="messageTemplate"/> placeholders.</param>
    /// <param name="propertyValue2">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<T0, T1, T2>(this ILogger logger, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
        => StartActivity(logger, LogEventLevel.Information, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
}
