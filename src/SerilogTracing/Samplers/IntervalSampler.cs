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
using SerilogTracing.Configuration;

namespace SerilogTracing.Samplers;

/// <summary>
/// Record one trace in every <c>N</c>.
/// </summary>
static class IntervalSampler
{
    /// <summary>
    /// Create a sampling delegate that records one trace in every <paramref name="interval"/> possible traces.
    /// </summary>
    /// <param name="interval">The sampling interval. Note that this is per root activity, not per individual activity.</param>
    /// <returns>A sampling function that can be provided to <see cref="ActivityListenerSamplingConfiguration.Using"/>.</returns>
    public static SampleActivity<ActivityContext> Create(ulong interval)
    {
        if (interval == 0) throw new ArgumentOutOfRangeException(nameof(interval));
        var next = (long)interval - 1;
        
        return (ref ActivityCreationOptions<ActivityContext> _) =>
        {
            // Tf the trace is not included in the sample, return `PropagationData` so that
            // we apply the same decision to child activities via the path above.
            var n = Interlocked.Increment(ref next) % (long)interval;
            return n == 0
                ? ActivitySamplingResult.AllDataAndRecorded
                : ActivitySamplingResult.PropagationData;
        };
    }
}