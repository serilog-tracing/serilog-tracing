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
    {
        return StartActivity(logger, LogEventLevel.Information, messageTemplate, propertyValues);
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
        if (logger == null! || messageTemplate == null!)
            return LoggerActivity.None;
        
        if (!logger.IsEnabled(level))
        {
            return LoggerActivity.None;
        }

        var activity = LoggerActivitySource.TryStartActivity(messageTemplate);
        if (activity == null)
        {
            return LoggerActivity.None;
        }

        if (!logger.BindMessageTemplate(messageTemplate, propertyValues, out var parsedTemplate, out var captures))
        {
            parsedTemplate = FallbackTemplate;
            captures = new[] { new LogEventProperty(FallbackTemplateOriginalTemplateName, new ScalarValue(messageTemplate)) };
        }

        return new LoggerActivity(logger, level, activity, parsedTemplate, captures);
    }
}