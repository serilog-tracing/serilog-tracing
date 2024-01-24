using Serilog;
using Serilog.Core;
using SerilogTracing.Interop;

// ReSharper disable MemberCanBePrivate.Global

namespace SerilogTracing;

/// <summary>
/// Extends <see cref="ILogger"/> with methods for creating activities that are emitted to the logger
/// as spans.
/// </summary>
public static class LoggerTracingExtensions
{
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
    public static LoggerActivity StartActivity(this ILogger logger, string messageTemplate, params object?[] propertyValues)
    {
        if (!logger.BindMessageTemplate(messageTemplate, propertyValues, out var parsedTemplate, out var captures))
            return LoggerActivity.None;
            
        var activity = LoggerActivitySource.StartActivity(messageTemplate);
        
        return new LoggerActivity(logger, activity, parsedTemplate, captures);
    }

    /// <summary>
    /// Start an activity.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    public static LoggerActivity StartActivity(this ILogger logger, string messageTemplate) 
        => StartActivity(logger, messageTemplate, []);

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
        => StartActivity(logger, messageTemplate, [propertyValue]);

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
        => StartActivity(logger, messageTemplate, [propertyValue0, propertyValue1]);

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
        => StartActivity(logger, messageTemplate, [propertyValue0, propertyValue1, propertyValue2]);
}