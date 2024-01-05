using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using SerilogTracing.Instrumentation;
using SerilogTracing.Interop;

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
        var activityListener = new ActivityListener();
        var diagnosticListenerSubscription = DiagnosticListener.AllListeners.Subscribe(new DiagnosticListenerObserver());
        var disposeProxy = new DisposeProxy(activityListener, diagnosticListenerSubscription);
        
        var logger = loggerConfiguration
            .WriteTo.Sink(disposeProxy)
            .CreateLogger();
        
        var options = new SerilogActivityListenerOptions();
        configure?.Invoke(options);
        
        ILogger GetLogger(string name)
        {
            return !string.IsNullOrWhiteSpace(name) ? logger.ForContext(Constants.SourceContextPropertyName, name) : logger;
        }
        
        activityListener.Sample = options.Sample;
        activityListener.SampleUsingParentId = options.SampleUsingParentId;
        activityListener.ShouldListenTo = source => GetLogger(source.Name).IsEnabled(LogEventLevel.Fatal);

        activityListener.ActivityStopped += activity =>
        {
            if (ActivityUtil.TryGetLoggerActivity(activity, out _))
                return; // `LoggerActivity` completion writes these to the activity-specific logger.

            var activityLogger = GetLogger(activity.Source.Name);

            var level = ActivityUtil.GetCompletionLevel(activity);
            if (!activityLogger.IsEnabled(level))
                return;

            activityLogger.Write(ActivityUtil.ActivityToLogEvent(activityLogger, activity));
        };
        
        ActivitySource.AddActivityListener(activityListener);

        return logger;
    }

    sealed class DisposeProxy(params IDisposable[] disposables) : ILogEventSink, IDisposable
    {
        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("TracingLogger: exception in dispose" + Environment.NewLine + ex);
                }
            }
        }

        void ILogEventSink.Emit(LogEvent logEvent)
        {
        }
    }
}
