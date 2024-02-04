namespace SerilogTracing.OpenTelemetry.Exporter;

internal class SerilogTraceExporter(ILogger logger) : BaseExporter<Activity>
{
    private readonly ActivityWriter _writer = new(() => logger);

    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
            _writer.Write(activity);

        return ExportResult.Success;
    }
}