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

    internal TracingSamplingConfiguration(TracingConfiguration tracingConfiguration)
    {
        _tracingConfiguration = tracingConfiguration;
    }

    internal void ConfigureSampling(ActivityListener listener)
    {
        if (_sample is null && _sampleUsingParentId is null)
        {
            listener.Sample = delegate { return ActivitySamplingResult.AllData; };
            return;
        }

        listener.Sample = _sample;
        listener.SampleUsingParentId = _sampleUsingParentId;
    }

    /// <summary>
    /// Set the sampling level for the listener. The <see cref="ActivityContext"/> supplied to
    /// the callback will contain the current trace id, and if the activity has a known parent
    /// a non-default span id identifying the parent.
    /// </summary>
    /// <param name="sample">A callback providing the sampling level for a particular activity.</param>
    /// <returns>The current instance, to enable method chaining.</returns>
    /// <remarks>If the method is called multiple times, the last sampling callback to be
    /// specified will be used.</remarks>
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
    /// <remarks>If the method is called multiple times, the last sampling callback to be
    /// specified will be used.</remarks>
    /// <seealso cref="ActivityListener.SampleUsingParentId"/>
    public TracingConfiguration UsingParentId(SampleActivity<string> sample)
    {
        _sampleUsingParentId = sample;
        return _tracingConfiguration;
    }
}
