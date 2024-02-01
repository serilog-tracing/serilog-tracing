using Serilog;
using Serilog.Configuration;
using SerilogTracing.Enrichers;

namespace SerilogTracing;

/// <summary>
/// Enrichment for spans.
/// </summary>
public static class TracingLoggerEnrichmentConfigurationExtensions
{
    /// <summary>
    /// Enrich log events with the span duration as milliseconds.
    /// </summary>
    /// <param name="enrichment">The configuration object.</param>
    /// <param name="propertyName">The name of the property to add.</param>
    public static LoggerConfiguration WithElapsedMilliseconds(this LoggerEnrichmentConfiguration enrichment, string? propertyName = null)
    {
        return enrichment.With(new ElapsedMilliseconds(propertyName ?? "Elapsed"));
    }
    
    /// <summary>
    /// Enrich log events with the span duration as a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="enrichment">The configuration object.</param>
    /// <param name="propertyName">The name of the property to add.</param>
    public static LoggerConfiguration WithElapsedTime(this LoggerEnrichmentConfiguration enrichment, string? propertyName = null)
    {
        return enrichment.With(new ElapsedTime(propertyName ?? "Elapsed"));
    }
}
