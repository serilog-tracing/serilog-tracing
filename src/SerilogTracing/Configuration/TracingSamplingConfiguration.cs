using System.Diagnostics;

namespace SerilogTracing.Configuration;

/// <summary>
/// Options for <see cref="TracingConfiguration"/> configuration.
/// </summary>
public class TracingSamplingConfiguration
{
    readonly TracingConfiguration _tracingConfiguration;
    SampleActivity<ActivityContext>? _sample;
    SampleActivity<string>? _sampleUsingParentId;
    
    internal SampleActivity<ActivityContext>? ActivityContext => _sample;
    internal SampleActivity<string>? ParentId => _sampleUsingParentId;

    internal TracingSamplingConfiguration(TracingConfiguration tracingConfiguration)
    {
        _tracingConfiguration = tracingConfiguration;
    }

    /// <summary>
    /// Set the sampling level for the listener. The <see cref="System.Diagnostics.ActivityContext"/> supplied to
    /// the callback will contain the current trace id, and if the activity has a known parent
    /// a non-default span id identifying the parent.
    /// </summary>
    /// <param name="sample">A callback providing the sampling level for a particular activity.</param>
    /// <returns>The current instance, to enable method chaining.</returns>
    /// <remarks>
    /// If the method is called multiple times, the last sampling callback to be specified will be used.
    /// This sampler will only run on activities that pass the <see cref="Serilog.ILogger.IsEnabled"/> filter
    /// on the destination logger.
    /// </remarks>
    /// <seealso cref="ActivityListener.Sample"/>
    public TracingConfiguration UsingActivityContext(SampleActivity<ActivityContext> sample)
    {
        _sample = sample;
        return _tracingConfiguration;
    }

    /// <summary>
    /// Set the sampling level for the listener. The string supplied to
    /// the callback will contain the current trace id.
    /// </summary>
    /// <param name="sample">A callback providing the sampling level for a particular activity.</param>
    /// <returns>The current instance, to enable method chaining.</returns>
    /// <remarks>
    /// If the method is called multiple times, the last sampling callback to be specified will be used.
    /// This sampler will only run on activities that pass the <see cref="Serilog.ILogger.IsEnabled"/> filter
    /// on the destination logger.
    /// </remarks>
    /// <seealso cref="ActivityListener.SampleUsingParentId"/>
    public TracingConfiguration UsingParentId(SampleActivity<string> sample)
    {
        _sampleUsingParentId = sample;
        return _tracingConfiguration;
    }
}
