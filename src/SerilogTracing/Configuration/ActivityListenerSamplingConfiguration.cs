using System.Diagnostics;

namespace SerilogTracing.Configuration;

/// <summary>
/// Options for <see cref="ActivityListenerConfiguration"/> configuration.
/// </summary>
public class ActivityListenerSamplingConfiguration
{
    readonly ActivityListenerConfiguration _activityListenerConfiguration;
    SampleActivity<ActivityContext>? _sample;
    
    internal SampleActivity<ActivityContext>? ActivityContext => _sample;

    internal ActivityListenerSamplingConfiguration(ActivityListenerConfiguration activityListenerConfiguration)
    {
        _activityListenerConfiguration = activityListenerConfiguration;
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
    public ActivityListenerConfiguration Using(SampleActivity<ActivityContext> sample)
    {
        _sample = sample;
        return _activityListenerConfiguration;
    }
}
