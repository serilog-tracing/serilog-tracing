using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing;

/// <summary>
/// An activity associated with a particular Serilog <see cref="ILogger"/>. When the activity
/// is completed, a span will be written through the logger. The activity also wraps an underlying
/// <see cref="System.Diagnostics.Activity"/> object that mirrors the properties of the
/// <see cref="LoggerActivity"/>, if any <c>System.Diagnostics</c> activity listeners are configured.
/// </summary>
/// <remarks><see cref="LoggerActivity"/> instances are not thread-safe.</remarks>
public sealed class LoggerActivity : IDisposable
{
    internal static LoggerActivity None { get; } = new(new LoggerConfiguration().CreateLogger(), null, new(Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>());

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

    internal MessageTemplate MessageTemplate { get; }
    internal DateTime StartTimestamp { get; }
    internal IEnumerable<LogEventProperty> Captures => _captures;
    internal Exception? Exception { get; private set; }
    internal LogEventLevel? CompletionLevel { get; private set; }
    internal TimeSpan Duration { get; private set; }

    internal ActivityTraceId? TraceId => Activity?.TraceId;
    internal ActivitySpanId? SpanId => Activity?.SpanId;
    internal ActivitySpanId? ParentSpanId => Activity?.ParentSpanId;

    /// <summary>
    /// The <see cref="Activity"/> that represents the current <see cref="LoggerActivity"/> for
    /// <c>System.Diagnostics</c>, if any listeners are configured.
    /// </summary>
    public Activity? Activity { get; }

    /// <summary>
    /// Add a property to the activity. This will be recorded in the emitted span.
    /// </summary>
    /// <remarks>If <see cref="Activity"/> is not null, the property value will also be
    /// attached to it as a tag. Note that when <paramref name="destructureObjects"/> is specified,
    /// the property value will be converted to a tag value using <see cref="Object.ToString"/>.</remarks>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="value">The value of the property.</param>
    /// <param name="destructureObjects">If <c langword="true">true</c>, Serilog's capturing
    /// logic will be used to serialize the object into a structured value.</param>
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
    
    /// <summary>
    /// Complete the activity, emitting a span to the underlying logger.
    /// </summary>
    /// <param name="level">The log event level to associate with the span.</param>
    /// <param name="exception">An exception to associate with the span, if any.</param>
    /// <remarks>Serilog levels will be reflected on the wrapped activity using
    /// corresponding <see cref="ActivityStatusCode"/> values. Exceptions are reflected using
    /// activity events. Once <see cref="Complete"/> has been called, subsequent calls will be
    /// ignored.
    /// </remarks>
    public void Complete(
        LogEventLevel level = LogEventLevel.Information,
        Exception? exception = null)
    {
        if (this == None
            || CompletionLevel.HasValue
#if FEATURE_ACTIVITY_ISSTOPPED
            || Activity?.IsStopped is true
#endif
            )
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
    
    /// <summary>
    /// Dispose the activity. This will call <see cref="Complete"/>, if the activity has not already been
    /// completed.
    /// </summary>
    public void Dispose()
    {
        Complete();
    }
}