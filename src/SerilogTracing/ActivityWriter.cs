using System.Diagnostics;
using Serilog;
using Serilog.Core;
using SerilogTracing.Instrumentation;
using SerilogTracing.Interop;

namespace SerilogTracing;

/// <summary>Handles writing of diagnostic activities to Serilog</summary>
public class ActivityWriter(Func<ILogger> logger)
{
    internal ILogger GetLogger(string name)
    {
        var instance = logger();
        return !string.IsNullOrWhiteSpace(name) ? instance.ForContext(Constants.SourceContextPropertyName, name) : instance;
    }

    /// <summary>Writes activity to Serilog</summary>
    /// <param name="activity">Activity to write</param>
    public void Write(Activity activity)
    {
        if (ActivityInstrumentation.HasAttachedLoggerActivity(activity))
            return;

        var activityLogger = GetLogger(activity.Source.Name);

        var level = ActivityInstrumentation.GetCompletionLevel(activity);
        if (!activityLogger.IsEnabled(level))
            return;

        activityLogger.Write(ActivityConvert.ActivityToLogEvent(activityLogger, activity));
    }
}