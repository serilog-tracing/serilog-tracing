using SerilogTracing.Instrumentation;

namespace SerilogTracing;

/// <summary>
/// Controls enrichment configuration.
/// </summary>
public sealed class InstrumentationOptions
{
    readonly ActivityListenerOptions _options;
    readonly Action<IActivityInstrumentor> _addInstrumentor;
    readonly Action<bool> _useDefaultInstrumentors;

    internal InstrumentationOptions(
        ActivityListenerOptions options,
        Action<IActivityInstrumentor> addInstrumentor,
        Action<bool> useDefaultInstrumentors)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _addInstrumentor = addInstrumentor ?? throw new ArgumentNullException(nameof(addInstrumentor));
        _useDefaultInstrumentors = useDefaultInstrumentors ?? throw new ArgumentNullException(nameof(useDefaultInstrumentors));
    }

    /// <summary>
    /// Whether to use default built-in activity instrumentors.
    /// </summary>
    /// <param name="withDefaults">If true, default activity instrumentors will be used.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public ActivityListenerOptions WithDefaultInstrumentation(bool withDefaults)
    {
        _useDefaultInstrumentors(withDefaults);

        return _options;
    }

    /// <summary>
    /// Specifies one or more instrumentors that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <param name="instrumentors">Instrumentors to apply to all events passing through
    /// the logger.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instrumentors"/> is <code>null</code></exception>
    /// <exception cref="ArgumentException">When any element of <paramref name="instrumentors"/> is <code>null</code></exception>
    public ActivityListenerOptions With(params IActivityInstrumentor[] instrumentors)
    {
        if (instrumentors == null)
        {
            throw new ArgumentNullException(nameof(instrumentors));
        }

        foreach (var instrumentor in instrumentors)
        {
            if (instrumentor == null)
            {
                throw new ArgumentNullException(nameof(instrumentors));
            }

            _addInstrumentor(instrumentor);
        }
        
        return _options;
    }

    /// <summary>
    /// Specifies an instrumentor that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <typeparam name="TInstrumentor">Instrumentor type to apply to all events passing through
    /// the logger.</typeparam>
    /// <returns>Configuration object allowing method chaining.</returns>
    public ActivityListenerOptions With<TInstrumentor>()
        where TInstrumentor : IActivityInstrumentor, new()
    {
        return With(new TInstrumentor());
    }
}