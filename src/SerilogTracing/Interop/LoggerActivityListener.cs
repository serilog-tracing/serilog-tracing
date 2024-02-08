using System.Diagnostics;
using Serilog;
using Serilog.Events;
using SerilogTracing.Core;
using SerilogTracing.Instrumentation;
using Constants = Serilog.Core.Constants;

namespace SerilogTracing.Interop;

sealed class LoggerActivityListener: IDisposable
{
    readonly ActivityListener? _listener;
    readonly DiagnosticListenerObserver? _subscription;

    LoggerActivityListener(ActivityListener? listener, DiagnosticListenerObserver? subscription)
    {
        _listener = listener;
        _subscription = subscription;
    }
    
    internal static LoggerActivityListener Configure(ActivityListenerConfiguration configuration, Func<ILogger> logger)
    {
        ILogger GetLogger(string name)
        {
            var instance = logger();
            return !string.IsNullOrWhiteSpace(name)
                ? instance.ForContext(Constants.SourceContextPropertyName, name)
                : instance;
        }

        var activityListener = new ActivityListener();
        var subscription = new DiagnosticListenerObserver(configuration.Instrument.GetInstrumentors().ToArray());

        try
        {
            var levelMap = configuration.InitialLevel.GetOverrideMap();

            // We may want an opt-in to performing level checks eagerly here.
            // It would be a performance win, but would also prevent dynamic log level changes from being effective.
            activityListener.ShouldListenTo = _ => true;

            var sample = configuration.Sample.ActivityContext;
            activityListener.Sample = (ref ActivityCreationOptions<ActivityContext> activity) =>
            {
                if (!GetLogger(activity.Source.Name)
                        .IsEnabled(GetInitialLevel(levelMap, activity.Source.Name)))
                    return ActivitySamplingResult.None;

                return sample?.Invoke(ref activity) ?? ActivitySamplingResult.AllData;
            };

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

            return new LoggerActivityListener(activityListener, subscription);
        }
        catch
        {
            activityListener.Dispose();
            subscription.Dispose();
            throw;
        }
    }
    
    static LogEventLevel GetInitialLevel(LevelOverrideMap levelMap, string activitySourceName)
    {
        levelMap.GetEffectiveLevel(activitySourceName, out var initialLevel, out var overrideLevel);

        return overrideLevel?.MinimumLevel ?? initialLevel;
    }

    static LogEventLevel GetCompletionLevel(LevelOverrideMap levelMap, Activity activity)
    {
        var level = GetInitialLevel(levelMap, activity.Source.Name);

        if (activity.Status == ActivityStatusCode.Error && level < LogEventLevel.Error)
        {
            return LogEventLevel.Error;
        }

        return level;
    }

    public void Dispose()
    {
        _listener?.Dispose();
        _subscription?.Dispose();
    }
}