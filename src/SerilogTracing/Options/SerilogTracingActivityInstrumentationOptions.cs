using SerilogTracing.Instrumentation;

namespace SerilogTracing.Options;

/// <summary>
/// Controls enrichment configuration.
/// </summary>
public sealed class SerilogTracingActivityInstrumentationOptions
{
    readonly SerilogTracingOptions _options;
    readonly Action<IActivityInstrumentor> _addInstrumentor;
    readonly Action<bool> _useDefaultInstrumentors;

    internal SerilogTracingActivityInstrumentationOptions(
        SerilogTracingOptions options,
        Action<IActivityInstrumentor> addEnricher,
        Action<bool> useDefaultEnrichers)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _addInstrumentor = addEnricher ?? throw new ArgumentNullException(nameof(addEnricher));
        _useDefaultInstrumentors = useDefaultEnrichers ?? throw new ArgumentNullException(nameof(useDefaultEnrichers));
    }

    /// <summary>
    /// Whether to use default built-in activity enrichers.
    /// </summary>
    /// <param name="withDefaults">If true, default activity enrichers will be used.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public SerilogTracingOptions WithDefaultInstrumentation(bool withDefaults)
    {
        _useDefaultInstrumentors(withDefaults);

        return _options;
    }

    /// <summary>
    /// Specifies one or more enrichers that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <param name="instrumentors">Enrichers to apply to all events passing through
    /// the logger.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instrumentors"/> is <code>null</code></exception>
    /// <exception cref="ArgumentException">When any element of <paramref name="instrumentors"/> is <code>null</code></exception>
    public SerilogTracingOptions With(params IActivityInstrumentor[] instrumentors)
    {
        if (instrumentors == null)
        {
            throw new ArgumentNullException(nameof(instrumentors));
        }

        foreach (var instrumentor in instrumentors)
        {
            if (instrumentor == null)
            {
                throw new ArgumentNullException(nameof(instrumentor));
            }

            _addInstrumentor(instrumentor);
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
        where TEnricher : IActivityInstrumentor, new()
    {
        return With(new TEnricher());
    }
}