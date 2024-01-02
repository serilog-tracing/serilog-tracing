using System.Diagnostics;

namespace SerilogTracing;

/// <summary>
/// Options for <see cref="LoggerConfigurationTracingExtensions"/> configuration.
/// </summary>
public class SerilogActivityListenerOptions
{
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
}
