using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SerilogTracing;

public class ActivityListenerConfiguration
{
    ILogger? _logger;

    public ActivityListenerConfiguration SetLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    public SerilogActivityListener CreateActivityListener()
    {
        var localLogger = _logger; // Avoid capturing the whole configuration object
        ILogger GetLogger() => localLogger ?? Log.Logger;
        
        var listener = new ActivityListener();
        listener.Sample = delegate { return ActivitySamplingResult.AllData; };
        listener.ShouldListenTo = source => 
            string.IsNullOrEmpty(source.Name) ? GetLogger().IsEnabled(LogEventLevel.Fatal) :
            GetLogger().ForContext(Constants.SourceContextPropertyName, source.Name).IsEnabled(LogEventLevel.Fatal);

        listener.ActivityStopped += activity =>
        {
            if (ActivityUtil.TryGetLoggerActivity(activity, out _))
                return; // `LoggerActivity` completion writes these to the activity-specific logger.
            
            var activityLogger = GetLogger().ForContext(Constants.SourceContextPropertyName, activity.Source.Name);

            var level = ActivityUtil.GetCompletionLevel(activity);
            if (!activityLogger.IsEnabled(level))
                return;

            activityLogger.Write(ActivityUtil.ActivityToLogEvent(activityLogger, activity));
        };
        
        ActivitySource.AddActivityListener(listener);

        return new SerilogActivityListener(listener);
    }
}
