using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using SerilogTracing.Configuration;
using SerilogTracing.Core;
using SerilogTracing.Instrumentation;
using SerilogTracing.Interop;
using Constants = Serilog.Core.Constants;

namespace SerilogTracing;

/// <summary>
/// Configure integration between SerilogTracing and the .NET tracing infrastructure.
/// </summary>
public class TracingConfiguration
{
    /// <summary>
    /// Construct a new <see cref="TracingConfiguration" />.
    /// </summary>
    public TracingConfiguration()
    {
        Instrument = new TracingInstrumentationConfiguration(this);
        Sample = new TracingSamplingConfiguration(this);
        InitialLevel = new TracingInitialLevelConfiguration(this);
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
    /// Configures the initial level assigned to externally created activities.
    /// </summary>
    public TracingInitialLevelConfiguration InitialLevel { get; }

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
    /// <param name="logger">
    /// The logger instance to emit traces through. Avoid using the shared <see cref="Log.Logger" /> as
    /// the value here. To emit traces through the shared static logger, call <see cref="TraceToSharedLogger" /> instead.
    /// </param>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceTo(ILogger logger)
    {
        return EnableTracing(() => logger);
    }

    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// This method is not equivalent to <code>TraceTo(Log.Logger)</code>. The former will emit traces through whatever the
    /// value of <see cref="Log.Logger" /> happened to be at the time <see cref="TraceTo" /> was called. This method
    /// will always emit traces through the current value of <see cref="Log.Logger" />.
    /// </summary>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceToSharedLogger()
    {
        return EnableTracing(() => Log.Logger);
    }

    IDisposable EnableTracing(Func<ILogger> logger)
    {
        ILogger GetLogger(string name)
        {
            var instance = logger();
            return !string.IsNullOrWhiteSpace(name)
                ? instance.ForContext(Constants.SourceContextPropertyName, name)
                : instance;
        }

        var activityListener = new ActivityListener();
        var disposeProxy = new DisposeProxy(activityListener,
            DiagnosticListener.AllListeners.Subscribe(
                new DiagnosticListenerObserver(Instrument.GetInstrumentors().ToArray())));

        var levelMap = InitialLevel.GetOverrideMap();

        // We may want an opt-in to performing level checks eagerly here.
        // It would be a performance win, but would also prevent dynamic log level changes from being effective.
        activityListener.ShouldListenTo = _ => true;

        var sample = Sample.ActivityContext;
        var usingParentId = Sample.ParentId;
        activityListener.Sample = (ref ActivityCreationOptions<ActivityContext> activity) =>
        {
            if (!GetLogger(activity.Source.Name)
                    .IsEnabled(GetInitialLevel(levelMap, activity.Source.Name)))
                return ActivitySamplingResult.None;

            return sample?.Invoke(ref activity) ?? ActivitySamplingResult.AllData;
        };

        // Only set this listener if the user supplied a parent id based sampler,
        // or if they didn't supply a context based one. It's treated preferentially, so if set
        // then the context based sampler will be ignored.
        if (usingParentId != null || sample == null)
        {
            activityListener.SampleUsingParentId = (ref ActivityCreationOptions<string> activity) =>
            {
                if (!GetLogger(activity.Source.Name)
                        .IsEnabled(GetInitialLevel(levelMap, activity.Source.Name)))
                    return ActivitySamplingResult.None;

                return usingParentId?.Invoke(ref activity) ?? ActivitySamplingResult.AllData;
            };
        }

        activityListener.ActivityStopped += activity =>
        {
            if (!activity.Recorded) return;
            
            if (ActivityInstrumentation.HasAttachedLoggerActivity(activity))
                return; // `LoggerActivity` completion writes these to the activity-specific logger.

            var activityLogger = GetLogger(activity.Source.Name);

            var level = GetCompletionLevel(levelMap, activity);

            if (!activityLogger.IsEnabled(level))
                return;

            activityLogger.Write(ActivityConvert.ActivityToLogEvent(activityLogger, activity, level));
        };

        ActivitySource.AddActivityListener(activityListener);

        return disposeProxy;
    }

    static LogEventLevel GetInitialLevel(LevelOverrideMap levelMap, string activitySourceName)
    {
        levelMap.GetEffectiveLevel(activitySourceName, out var initialLevel, out var overrideLevel);

        return overrideLevel?.MinimumLevel ?? initialLevel;
    }

    static LogEventLevel GetCompletionLevel(LevelOverrideMap levelMap, Activity activity)
    {
        var level = ActivityInstrumentation.GetCompletionLevel(activity);
        var overrideLevel = GetInitialLevel(levelMap, activity.Source.Name);

        return overrideLevel > level ? overrideLevel : level;
    }

    sealed class DisposeProxy(params IDisposable[] disposables) : IDisposable
    {
        public void Dispose()
        {
            foreach (var disposable in disposables)
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