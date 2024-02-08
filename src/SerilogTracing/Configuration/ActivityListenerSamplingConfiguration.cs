// Copyright © SerilogTracing Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
