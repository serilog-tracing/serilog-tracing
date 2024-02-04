using SerilogTracing.OpenTelemetry.Exporter;

// ReSharper disable once CheckNamespace
namespace OpenTelemetry.Trace;

/// <summary>
/// Extension methods to simplify registering of the Serilog exporter.
/// </summary>
public static class SerilogTraceExporterHelperExtensions
{
    /// <summary>
    /// Adds Serilog exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="logger"><see cref="ILogger"/> to output traces to</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddSerilogExporter(this TracerProviderBuilder builder, ILogger logger)
        => builder.AddProcessor(new SimpleActivityExportProcessor(new SerilogTraceExporter(logger)));
}