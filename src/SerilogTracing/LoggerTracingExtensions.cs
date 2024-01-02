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
        return StartActivity<Logger>(logger, messageTemplate, propertyValues);
    }

    /// <summary>
    /// Start an activity, using <typeparamref name="TSource"/> to name the
    /// underlying <see cref="System.Diagnostics.ActivitySource"/>, and to override the logger's source context.
    /// </summary>
    /// <param name="logger">The logger that the resulting span will be written to, when the activity
    /// is completed using <see cref="LoggerActivity.Complete"/> or <see cref="LoggerActivity.Dispose"/>.</param>
    /// <param name="messageTemplate">A message template that will be used to format the activity name.</param>
    /// <param name="propertyValues">Values to substitute into the <paramref name="messageTemplate"/> placeholders.
    /// These properties will also be attached to the resulting span.</param>
    /// <returns>A <see cref="LoggerActivity"/>.</returns>
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<TSource>(this ILogger logger, string messageTemplate, params object?[] propertyValues)
    {
        var activity = LoggerActivitySource<TSource>.Instance.StartActivity(messageTemplate);

        if (!logger.BindMessageTemplate(messageTemplate, propertyValues, out var parsedTemplate, out var captures))
            return LoggerActivity.None;
            
        return new LoggerActivity(logger, activity, parsedTemplate, captures);
    }
}