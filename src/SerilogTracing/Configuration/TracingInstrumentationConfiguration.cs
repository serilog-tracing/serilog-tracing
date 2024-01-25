using SerilogTracing.Instrumentation;
using SerilogTracing.Instrumentation.HttpClient;

namespace SerilogTracing.Configuration;

/// <summary>
/// Controls instrumentation configuration.
/// </summary>
public sealed class TracingInstrumentationConfiguration
{
    readonly TracingConfiguration _tracingConfiguration;
    readonly List<IActivityInstrumentor> _instrumentors = [];
    bool _withDefaultInstrumentors = true;
    
    static IEnumerable<IActivityInstrumentor> GetDefaultInstrumentors() => [new HttpRequestOutActivityInstrumentor()];
    
    internal IEnumerable<IActivityInstrumentor> GetInstrumentors() =>
        _withDefaultInstrumentors ?
            GetDefaultInstrumentors().Concat(_instrumentors) : _instrumentors;
    
    internal TracingInstrumentationConfiguration(TracingConfiguration tracingConfiguration)
    {
        _tracingConfiguration = tracingConfiguration;
    }

    /// <summary>
    /// Whether to use default built-in activity instrumentors.
    /// </summary>
    /// <param name="withDefaults">If true, default activity instrumentors will be used.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public TracingConfiguration WithDefaultInstrumentation(bool withDefaults)
    {
        _withDefaultInstrumentors = withDefaults;
        return _tracingConfiguration;
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
    public TracingConfiguration With(params IActivityInstrumentor[] instrumentors)
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

            _instrumentors.Add(instrumentor);
        }
        
        return _tracingConfiguration;
    }

    /// <summary>
    /// Specifies an instrumentor that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <typeparam name="TInstrumentor">Instrumentor type to apply to all events passing through
    /// the logger.</typeparam>
    /// <returns>Configuration object allowing method chaining.</returns>
    public TracingConfiguration With<TInstrumentor>()
        where TInstrumentor : IActivityInstrumentor, new()
    {
        return With(new TInstrumentor());
    }
}