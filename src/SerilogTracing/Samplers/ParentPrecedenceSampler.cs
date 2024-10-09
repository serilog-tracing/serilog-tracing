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
/// A sampling wrapper that gives precedence to the sampling decision made for the parent activity.
/// </summary>
static class ParentPrecedenceSampler
{
    /// <summary>
    /// Create a sampling delegate that gives precedence to the sampling decision made for the parent activity, if present, but
    /// otherwise delegates the sampling decision to <see paramref="sampleRootActivity"/>.
    /// </summary>
    /// <param name="sampleRootActivity">The sampler that will be used when no parent activity is present.</param>
    /// <returns>A sampling function that can be provided to <see cref="ActivityListenerSamplingConfiguration.Using"/>.</returns>
    public static SampleActivity<ActivityContext> Create(SampleActivity<ActivityContext> sampleRootActivity)
    {
        return (ref ActivityCreationOptions<ActivityContext> options) =>
        {
            if (options.Parent != default)
            {
                // The activity is a child of another; if the parent is recorded, the child is recorded. Otherwise,
                // as long as a local activity is present, there's no need to generate an activity at all.
                return (options.Parent.TraceFlags & ActivityTraceFlags.Recorded) == ActivityTraceFlags.Recorded ?
                    ActivitySamplingResult.AllDataAndRecorded :
                    options.Parent.IsRemote ?
                        ActivitySamplingResult.PropagationData :
                        ActivitySamplingResult.None;
            }

            // We're at the root; apply the nested sampler.
            return sampleRootActivity(ref options);
        };
    }
}