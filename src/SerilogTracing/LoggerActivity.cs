using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Instrumentation;
using SerilogTracing.Interop;

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
    /// <summary>
    /// A <see cref="LoggerActivity"/> that represents a suppressed activity. The <see cref="Activity"/> property of
    /// this instance, and only this instance, will be <c langword="null">null</c>.
    /// </summary>
    public static LoggerActivity None { get; } = new(new LoggerConfiguration().CreateLogger(), LevelAlias.Minimum, null, new(Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>());

    internal LoggerActivity(
        ILogger logger,
        LogEventLevel defaultCompletionLevel,
        Activity? activity,
        MessageTemplate messageTemplate,
        IEnumerable<LogEventProperty> captures)
    {
        Logger = logger;
        DefaultCompletionLevel = defaultCompletionLevel;
        Activity = activity;
        MessageTemplate = messageTemplate;
        Properties = [];

        foreach (var capture in captures)
        {
            Properties[capture.Name] = capture;
        }

        if (activity != null)
        {
            ActivityInstrumentation.AttachLoggerActivity(activity, this);
        }
    }

    ILogger Logger { get; }
    LogEventLevel DefaultCompletionLevel { get; set; }
    bool IsComplete { get; set; }

    internal MessageTemplate MessageTemplate { get; }
    internal Dictionary<string, LogEventProperty> Properties { get; }
    
    bool IsSuppressed => ActivityInstrumentation.IsSuppressed(Activity) || IsComplete;

    bool IsDataSuppressed => ActivityInstrumentation.IsDataSuppressed(Activity) || IsComplete;

    /// <summary>
    /// The <see cref="Activity"/> that represents the current <see cref="LoggerActivity"/> for
    /// <c>System.Diagnostics</c>. This property is null if and only if the current <see cref="LoggerActivity"/> is
    /// suppressed, either through level checks or sampling.
    /// </summary>
    public Activity? Activity { get; }

    /// <summary>
    /// Add a property to the activity. This will be recorded in the emitted span.
    /// </summary>
    /// <remarks>If <see cref="Activity"/> is not null and <see cref="System.Diagnostics.Activity.IsAllDataRequested"/>
    /// is true, then the property value will be attached to it as a tag. Note that when <paramref name="destructureObjects"/> is specified,
    /// the property value will be converted to a tag value using <see cref="Object.ToString"/>.</remarks>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="value">The value of the property.</param>
    /// <param name="destructureObjects">If <c langword="true">true</c>, Serilog's capturing
    /// logic will be used to serialize the object into a structured value.</param>
    public void AddProperty(string propertyName, object? value, bool destructureObjects = false)
    {
        if (IsDataSuppressed)
        {
            return;
        }
        
        if (Logger.BindProperty(propertyName, value, destructureObjects, out var property))
        {
            ActivityInstrumentation.SetLogEventProperty(Activity!, property);
        }
    }
    
    /// <summary>
    /// Complete the activity, emitting a span to the underlying logger.
    /// </summary>
    /// <param name="level">By default, the level used when starting the activity will be used at completion. Specifying
    /// a level here will override the original completion level, but only if <paramref name="level"/> is higher than
    /// the original, for example to promote an <see cref="LogEventLevel.Information"/> event to a <see cref="LogEventLevel.Warning"/>
    /// event. If the level specified here is lower, it will be ignored.</param>
    /// <param name="exception">An exception to associate with the span, if any.</param>
    /// <remarks>Serilog levels will be reflected on the wrapped activity using
    /// corresponding <see cref="ActivityStatusCode"/> values. Exceptions are reflected using
    /// activity events. Once <see cref="Complete"/> has been called, subsequent calls will be
    /// ignored.
    /// </remarks>
    public void Complete(
        LogEventLevel? level = null,
        Exception? exception = null)
    {
        CompleteInternal(true, level, exception);
    }
    
    void CompleteInternal(
        bool isExplicit,
        LogEventLevel? level = null,
        Exception? exception = null)
    {
        if (IsSuppressed
#if FEATURE_ACTIVITY_ISSTOPPED
            // Though it could be considered misuse, avoid failures when the underlying activity
            // has been manually stopped/disposed outside of SerilogTracing.
            || Activity!.IsStopped
#endif
           )
        {
            return;
        }

        // This property can be removed once we can rely on the existence of Activity.IsStopped.
        IsComplete = true;
        var end = DateTimeOffset.Now;

        var completionLevel = DefaultCompletionLevel;
        if (level is { } completionLevelOverride && completionLevelOverride > completionLevel)
        {
            completionLevel = completionLevelOverride;
        }
        
        // The next half-dozen lines ensure other listeners see all of the info we have about the activity.
        
        if (exception != null)
        {
            ActivityInstrumentation.TrySetException(Activity!, exception);
        }

        // Only set the activity status if completion was done explicitly by the caller
        // If the activity was disposed without completing then leave the status unset
        if (isExplicit)
        {
            Activity!.SetStatus(completionLevel <= LogEventLevel.Warning
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);
        }

        Activity!.SetEndTime(end.UtcDateTime);
        Activity!.Stop();

        // We assume here that `level` is still enabled as it was in the call to `StartActivity()`. If this is not
        // the case, traces may end up with missing spans. Writing a `SelfLog` event would be reasonable but this
        // will end up being a hot path so avoiding it at this time.
        
        Logger.Write(ActivityConvert.ActivityToLogEvent(Logger, this, end, completionLevel, exception));
    }
    
    /// <summary>
    /// Dispose the activity. This will call <see cref="Complete"/>, if the activity has not already been
    /// completed.
    /// </summary>
    public void Dispose()
    {
        CompleteInternal(isExplicit: false);
    }
}