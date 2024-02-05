﻿using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using SerilogTracing.Configuration;
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
    [Obsolete("Use TraceTo(ILogger) or TraceToSharedLogger()")]
    public IDisposable EnableTracing(ILogger? logger = null)
    {
        return logger != null ? TraceTo(logger) : TraceToSharedLogger();
    }

    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// </summary>
    /// <param name="logger">The logger instance to emit traces through. Avoid using the shared <see cref="Log.Logger"/> as
    /// the value here. To emit traces through the shared static logger, call <see cref="TraceToSharedLogger"/> instead.</param>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceTo(ILogger logger)
    {
        return EnableTracing(() => logger);
    }

    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    ///
    /// This method is not equivalent to <code>TraceTo(Log.Logger)</code>. The former will emit traces through whatever the
    /// value of <see cref="Log.Logger"/> happened to be at the time <see cref="TraceTo"/> was called. This method
    /// will always emit traces through the current value of <see cref="Log.Logger"/>.
    /// </summary>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceToSharedLogger()
    {
        return EnableTracing(() => Log.Logger);
    }

    IDisposable EnableTracing(Func<ILogger> logger)
    {
        var instrumentors = Instrument.GetInstrumentors().ToArray();
        var activityListener = new ActivityListener();
        var diagnosticListenerSubscription = DiagnosticListener.AllListeners.Subscribe(new DiagnosticListenerObserver(instrumentors));
        var disposeProxy = new DisposeProxy(diagnosticListenerSubscription, activityListener);

        var writer = new ActivityWriter(logger);

        Sample.ConfigureSampling(activityListener);
        
        // Note, this will not be reevaluated if the minimum level dynamically changes.
        activityListener.ShouldListenTo = source => source.Name == Core.Constants.SerilogActivitySourceName || writer.GetLogger(source.Name).IsEnabled(LogEventLevel.Fatal);

        activityListener.ActivityStopped += writer.Write;
        
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
