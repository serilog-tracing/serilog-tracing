using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SerilogTracing;

/// <summary>
/// Configure an activity listener that writes completed spans to a Serilog logger.
/// </summary>
public static class LoggerConfigurationTracingExtensions
{
    /// <summary>
    /// Configure and register an activity listener that writes completed spans to a Serilog logger.
    /// The returned listener will continue writing spans through the Serilog logger until it is disposed.
    /// </summary>
    /// <returns>The logger.</returns>
    public static Logger CreateTracingLogger(this LoggerConfiguration loggerConfiguration, Action<SerilogActivityListenerOptions>? configure = null)
    {
        var listener = new ActivityListener();
        var disposeProxy = new DisposeProxy(listener);
        var logger = loggerConfiguration
            .Destructure.With(disposeProxy)
            .CreateLogger();
        
        var options = new SerilogActivityListenerOptions();
        configure?.Invoke(options);
        
        ILogger GetLogger(string name)
        {
            return !string.IsNullOrWhiteSpace(name) ? logger.ForContext(Constants.SourceContextPropertyName, name) : logger;
        }
        
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

        return logger;
    }

    sealed class DisposeProxy(IDisposable disposable) : IDestructuringPolicy, IDisposable
    {
        public void Dispose() => disposable.Dispose();

        bool IDestructuringPolicy.TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory,
            [NotNullWhen(true)] out LogEventPropertyValue? result)
        {
            result = null;
            return false;
        }
    }
}
