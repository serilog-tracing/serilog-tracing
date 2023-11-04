using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing;

public sealed class LoggerActivity : IDisposable
{
    public static LoggerActivity None { get; } = new(new LoggerConfiguration().CreateLogger(), null, new(Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>());

    IEnumerable<LogEventProperty> _captures;
    
    internal LoggerActivity(
        ILogger logger,
        Activity? activity,
        MessageTemplate messageTemplate,
        IEnumerable<LogEventProperty> captures)
    {
        Logger = logger;
        Activity = activity;
        MessageTemplate = messageTemplate;
        _captures = captures;

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
    public IEnumerable<LogEventProperty> Captures => _captures;
    public Exception? Exception { get; private set; }
    public LogEventLevel? CompletionLevel { get; private set; }

    public DateTime StartTimestamp { get; }
    public TimeSpan Duration { get; private set; }

    public ActivityTraceId? TraceId => Activity?.TraceId;
    public ActivitySpanId? SpanId => Activity?.SpanId;
    public ActivitySpanId? ParentSpanId => Activity?.ParentSpanId;

    public void AddProperty(string propertyName, object? value, bool destructureObjects = false)
    {
        if (Logger.BindProperty(propertyName, value, destructureObjects, out var property))
        {
            // May be best to split storage across the initial `IEnumerable` and a lazily-allocated
            // `List` to avoid this without copying `captures` in the constructor.
            _captures = _captures.Concat(new[] { property });
        }

        // In cases where `destructureObjects` is `true`, it's unlikely that the value will
        // be an immutable scalar suitable for using with `AddTag()`, so we avoid surprises
        // and stringify it in those cases.
        Activity?.AddTag(propertyName, destructureObjects ? value?.ToString() : value);
    }
    
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