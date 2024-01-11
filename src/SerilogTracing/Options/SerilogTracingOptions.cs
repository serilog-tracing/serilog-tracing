using System.Diagnostics;
using SerilogTracing.Instrumentation;

namespace SerilogTracing.Options;

/// <summary>
/// Options for <see cref="LoggerConfigurationTracingExtensions"/> configuration.
/// </summary>
public sealed class SerilogTracingOptions
{
    static readonly List<IActivityInstrumentor> DefaultInstrumentors = new()
    {
        new HttpRequestOutActivityInstrumentor()
    };
    
    readonly List<IActivityInstrumentor> _instrumentors = new();
    bool _withDefaultInstrumentors = true;

    internal SerilogTracingOptions()
    {
        Instrument = new SerilogTracingActivityInstrumentationOptions(this, instrumentor => _instrumentors.Add(instrumentor), withDefaults => _withDefaultInstrumentors = withDefaults);
    }

    internal IEnumerable<IActivityInstrumentor> Instrumentors => _withDefaultInstrumentors ? DefaultInstrumentors.Concat(_instrumentors) : _instrumentors;
    
    /// <summary>
    /// Set the sampling level for the listener. The <see cref="ActivityContext"/> supplied to
    /// the callback will contain the current trace id, and if the activity has a known parent
    /// a non-default span id identifying the parent.
    /// </summary>
    /// <param name="value">A callback providing the sampling level for a particular activity.</param>
    /// <returns>The current instance, to enable method chaining.</returns>
    /// <remarks>If the method is called multiple times, the last sampling callback to be
    /// specified will be used.</remarks>
    /// <seealso cref="ActivityListener.Sample"/>
    public SampleActivity<ActivityContext> Sample { get; set; } = delegate { return ActivitySamplingResult.AllData; };

    /// <summary>
    /// Set the sampling level for the listener. The string supplied to
    /// the callback will contain the current trace id.
    /// </summary>
    /// <param name="value">A callback providing the sampling level for a particular activity.</param>
    /// <returns>The current instance, to enable method chaining.</returns>
    /// <remarks>If the method is called multiple times, the last sampling callback to be
    /// specified will be used.</remarks>
    /// <seealso cref="ActivityListener.SampleUsingParentId"/>
    public SampleActivity<string> SampleUsingParentId { get; set; } = delegate { return ActivitySamplingResult.AllData; };
    
    /// <summary>
    /// Configures enrichment of <see cref="Activity">Activities</see>.
    /// </summary>
    public SerilogTracingActivityInstrumentationOptions Instrument { get; internal set; }
}
