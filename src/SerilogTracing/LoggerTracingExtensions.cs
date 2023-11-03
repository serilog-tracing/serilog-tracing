using Serilog;
using Serilog.Core;

namespace SerilogTracing;

public static class LoggerTracingExtensions
{
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity(this ILogger logger, string messageTemplate, params object?[] propertyValues)
    {
        return StartActivity<Logger>(logger, messageTemplate, propertyValues);
    }

    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity<TLogger>(this ILogger logger, string messageTemplate, params object?[] propertyValues)
    {
        var activity = SerilogActivitySource<TLogger>.Instance.StartActivity(messageTemplate);

        if (!logger.BindMessageTemplate(messageTemplate, propertyValues, out var parsedTemplate, out var captures))
            return LoggerActivity.None;
            
        return new LoggerActivity(logger, activity, parsedTemplate, captures);
    }
}