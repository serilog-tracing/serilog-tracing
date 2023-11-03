using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing;

public sealed class LoggerActivity : IDisposable
{
    public static LoggerActivity None { get; } = new(new LoggerConfiguration().CreateLogger(), null, new(Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>());

    internal LoggerActivity(
        ILogger logger,
        Activity? activity,
        MessageTemplate messageTemplate,
        IEnumerable<LogEventProperty> captures)
    {
        Logger = logger;
        Activity = activity;
        MessageTemplate = messageTemplate;
        Captures = captures;

        if (activity != null)
        {
            ActivityUtil.SetLoggerActivity(activity, this);
            StartTimestamp = activity.StartTimeUtc;
        }
        else
        {
            StartTimestamp = DateTime.UtcNow;
        }
    }

    ILogger Logger { get; }
    public Activity? Activity { get; }
    public MessageTemplate MessageTemplate { get; }
    public IEnumerable<LogEventProperty> Captures { get; }
    public Exception? Exception { get; private set; }
    public LogEventLevel? CompletionLevel { get; private set; }

    public DateTime StartTimestamp { get; }
    public TimeSpan Duration { get; set; }

    public ActivityTraceId? TraceId => Activity?.TraceId;
    public ActivitySpanId? SpanId => Activity?.SpanId;
    public ActivitySpanId? ParentSpanId => Activity?.ParentSpanId;

    public void Complete(
        LogEventLevel level = LogEventLevel.Information,
        Exception? exception = null)
    {
        if (this == None || Activity?.IsStopped is true)
        {
            return;
        }
        
        CompletionLevel = level;
        if (exception != null)
        {
            Exception = exception;
            Activity?.AddEvent(ActivityUtil.EventFromException(exception));
        }

        Activity?.SetStatus(level <= LogEventLevel.Warning ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        Activity?.Stop();

        Duration = Activity?.Duration ?? DateTime.UtcNow - StartTimestamp;
        
        Logger.Write(ActivityUtil.ActivityToLogEvent(Logger, this));
    }
    
    public void Dispose()
    {
        Complete();
    }
}