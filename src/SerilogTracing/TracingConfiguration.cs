using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using SerilogTracing.Instrumentation;
using SerilogTracing.Interop;

namespace SerilogTracing;

/// <summary>
/// Configure integration between SerilogTracing and the .NET tracing infrastructure.
/// </summary>
public class TracingConfiguration
{
    /// <summary>
    /// Construct a new <see cref="TracingConfiguration"/>.
    /// </summary>
    public TracingConfiguration()
    {
        Instrument = new TracingInstrumentationConfiguration(this);
        Sample = new TracingSamplingConfiguration(this);
    }

    /// <summary>
    /// Configures instrumentation of <see cref="Activity">activities</see>.
    /// </summary>
    public TracingInstrumentationConfiguration Instrument { get; }
    
    /// <summary>
    /// Configures sampling.
    /// </summary>
    public TracingSamplingConfiguration Sample { get; }
    
    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// </summary>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable EnableTracing(ILogger? logger = null)
    {
        var instrumentors = Instrument.GetInstrumentors().ToArray();
        var activityListener = new ActivityListener();
        var diagnosticListenerSubscription = DiagnosticListener.AllListeners.Subscribe(new DiagnosticListenerObserver(instrumentors));
        var disposeProxy = new DisposeProxy(diagnosticListenerSubscription, activityListener);

        ILogger GetLogger(string name)
        {
            var instance = logger ?? Log.Logger;
            return !string.IsNullOrWhiteSpace(name) ? instance.ForContext(Constants.SourceContextPropertyName, name) : instance;
        }

        Sample.ConfigureSampling(activityListener);
        activityListener.ShouldListenTo = source => GetLogger(source.Name).IsEnabled(LogEventLevel.Fatal);

        activityListener.ActivityStopped += activity =>
        {
            if (LoggerActivity.TryGetLoggerActivity(activity, out _))
                return; // `LoggerActivity` completion writes these to the activity-specific logger.

            var activityLogger = GetLogger(activity.Source.Name);

            var level = ActivityInstrumentation.GetCompletionLevel(activity);
            if (!activityLogger.IsEnabled(level))
                return;

            activityLogger.Write(ActivityConvert.ActivityToLogEvent(activityLogger, activity));
        };
        
        ActivitySource.AddActivityListener(activityListener);

        return disposeProxy;
    }

    sealed class DisposeProxy(params IDisposable[] disposables) : IDisposable
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
    }
}
