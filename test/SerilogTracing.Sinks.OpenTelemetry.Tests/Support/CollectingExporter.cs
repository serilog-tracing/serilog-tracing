using System.Diagnostics;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace SerilogTracing.Sinks.OpenTelemetry.Tests.Support;

class CollectingExporter : IExporter
{
    public List<ExportLogsServiceRequest> LogsServiceRequests { get; } = new();
    public List<ExportTraceServiceRequest> TraceServiceRequests { get; } = new();

    public void Export(ExportLogsServiceRequest request)
    {
        LogsServiceRequests.Add(request);
    }

    public Task ExportAsync(ExportLogsServiceRequest request)
    {
        Export(request);
        return Task.CompletedTask;
    }

    public void Export(ExportTraceServiceRequest request)
    {
        TraceServiceRequests.Add(request);
    }

    public Task ExportAsync(ExportTraceServiceRequest request)
    {
        Export(request);
        return Task.CompletedTask;
    }
}