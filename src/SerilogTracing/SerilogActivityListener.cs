using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SerilogTracing;

/// <summary>
/// Configure an activity listener that writes completed spans to a Serilog logger.
/// </summary>
public static class SerilogActivityListener
{
    /// <summary>
    /// Configure and register an activity listener that writes completed spans to a Serilog logger.
    /// The returned listener will continue writing spans through the Serilog logger until it is disposed.
    /// </summary>
    /// <returns>The configured, registered activity listener.</returns>
    public static ActivityListener Create(Action<SerilogActivityListenerOptions>? configure = null)
    {
        var options = new SerilogActivityListenerOptions();
        configure?.Invoke(options);
        
        // Don't capture or observe changes to the options object.
        var localLogger = options.Logger;

        ILogger GetLogger(string name)
        {
            var logger = localLogger ?? Log.Logger;
            return !string.IsNullOrWhiteSpace(name) ? logger.ForContext(Constants.SourceContextPropertyName, name) : logger;
        }
        
        var listener = new ActivityListener();
        listener.Sample = options.Sample;
        listener.SampleUsingParentId = options.SampleUsingParentId;
        listener.ShouldListenTo = source => GetLogger(source.Name).IsEnabled(LogEventLevel.Fatal);

        listener.ActivityStopped += activity =>
        {
            if (ActivityUtil.TryGetLoggerActivity(activity, out _))
                return; // `LoggerActivity` completion writes these to the activity-specific logger.

            var activityLogger = GetLogger(activity.Source.Name);

            var level = ActivityUtil.GetCompletionLevel(activity);
            if (!activityLogger.IsEnabled(level))
                return;

            activityLogger.Write(ActivityUtil.ActivityToLogEvent(activityLogger, activity));
        };
        
        ActivitySource.AddActivityListener(listener);

        return listener;
    }
}