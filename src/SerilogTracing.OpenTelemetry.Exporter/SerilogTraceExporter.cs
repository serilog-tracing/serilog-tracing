using SerilogTracing.Instrumentation;
using SerilogTracing.Interop;

namespace SerilogTracing.OpenTelemetry.Exporter;

internal class SerilogTraceExporter(ILogger logger) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            if (ActivityInstrumentation.HasAttachedLoggerActivity(activity))
                continue; // `LoggerActivity` completion writes these to the activity-specific logger.

            var activityLogger = !string.IsNullOrWhiteSpace(activity.Source.Name)
                ? logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, activity.Source.Name)
                : logger;

            var level = ActivityInstrumentation.GetCompletionLevel(activity);
            if (!activityLogger.IsEnabled(level))
                continue;

            activityLogger.Write(ActivityConvert.ActivityToLogEvent(activityLogger, activity));
        }

        return ExportResult.Success;
    }
}