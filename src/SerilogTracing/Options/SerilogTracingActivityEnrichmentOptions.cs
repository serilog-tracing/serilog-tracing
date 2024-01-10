using SerilogTracing.Instrumentation;

namespace SerilogTracing.Options;

/// <summary>
/// Controls enrichment configuration.
/// </summary>
public sealed class SerilogTracingActivityEnrichmentOptions
{
    readonly SerilogTracingOptions _options;
    readonly Action<IActivityEnricher> _addEnricher;

    internal SerilogTracingActivityEnrichmentOptions(
        SerilogTracingOptions options,
        Action<IActivityEnricher> addEnricher)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _addEnricher = addEnricher ?? throw new ArgumentNullException(nameof(addEnricher));
    }

    /// <summary>
    /// Specifies one or more enrichers that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <param name="enrichers">Enrichers to apply to all events passing through
    /// the logger.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="enrichers"/> is <code>null</code></exception>
    /// <exception cref="ArgumentException">When any element of <paramref name="enrichers"/> is <code>null</code></exception>
    public SerilogTracingOptions With(params IActivityEnricher[] enrichers)
    {
        if (enrichers == null)
        {
            throw new ArgumentNullException(nameof(enrichers));
        }

        foreach (var activityEnricher in enrichers)
        {
            if (activityEnricher == null)
            {
                throw new ArgumentNullException(nameof(enrichers));
            }

            _addEnricher(activityEnricher);
        }
        
        return _options;
    }

    /// <summary>
    /// Specifies an enricher that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <typeparam name="TEnricher">Enricher type to apply to all events passing through
    /// the logger.</typeparam>
    /// <returns>Configuration object allowing method chaining.</returns>
    public SerilogTracingOptions With<TEnricher>()
        where TEnricher : IActivityEnricher, new()
    {
        return With(new TEnricher());
    }
}