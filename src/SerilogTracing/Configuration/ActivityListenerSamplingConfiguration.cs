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
using SerilogTracing.Samplers;

namespace SerilogTracing.Configuration;

/// <summary>
/// Options for <see cref="ActivityListenerConfiguration"/> configuration.
/// </summary>
public class ActivityListenerSamplingConfiguration
{
    readonly ActivityListenerConfiguration _activityListenerConfiguration;
    SampleActivity<ActivityContext> _sample;

    internal SampleActivity<ActivityContext> SamplingDelegate => _sample;

    internal ActivityListenerSamplingConfiguration(ActivityListenerConfiguration activityListenerConfiguration)
    {
        _activityListenerConfiguration = activityListenerConfiguration;
        _sample = ParentPrecedentSampler.Create(AlwaysRecordedSampler.Create());
    }

    /// <summary>
    /// Set the sampling policy for the listener. The <see cref="System.Diagnostics.ActivityContext"/> supplied to
    /// the callback will contain the current trace id, and if the activity has a known parent
    /// a non-default span id identifying the parent.
    /// </summary>
    /// <param name="sample">A callback providing the sampling level for a particular activity.</param>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
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

    /// <summary>
    /// Record all traces. This is the SerilogTracing default policy.
    /// </summary>
    /// <remarks>This policy will respect any sampling decisions already made for parent activities. This will only
    /// occur when incoming requests or messages contain propagated tracing information; to control whether
    /// external tracing decisions are trusted, see for example <c>IncomingTraceParent</c> in the ASP.NET Core
    /// instrumentation.</remarks>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration AllTraces()
    {
        _sample = ParentPrecedentSampler.Create(AlwaysRecordedSampler.Create());
        return _activityListenerConfiguration;
    }

    /// <summary>
    /// Record one trace in every <paramref name="interval"/> possible traces. The sampling algorithm uses a simple local
    /// counter, which is not preserved across application restarts.
    /// </summary>
    /// <param name="interval">The sampling interval. Note that this is per root activity, not per individual activity.</param>
    /// <remarks>This policy will respect any sampling decisions already made for parent activities. This will only
    /// occur when incoming requests or messages contain propagated tracing information; to control whether
    /// external tracing decisions are trusted, see for example <c>IncomingTraceParent</c> in the ASP.NET Core
    /// instrumentation.</remarks>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration OneTraceIn(ulong interval)
    {
        _sample = ParentPrecedentSampler.Create(IntervalSampler.Create(interval));
        return _activityListenerConfiguration;
    }
}
