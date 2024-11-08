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
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// Instrument <see cref="Activity">activities</see> from an <see cref="System.Diagnostics.ActivitySource" />.
/// </summary>
public abstract class ActivitySourceInstrumentor : IActivityInstrumentor
{
    /// <summary>
    /// Whether the instrumentor should subscribe to events from the given <see cref="ActivitySource"/>.
    /// </summary>
    /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> of the candidate <see cref="ActivitySource"/>.</param>
    /// <returns>Whether the instrumentor should instrument activities from the given source.</returns>
    protected abstract bool ShouldSubscribeTo(string activitySourceName);

    /// <summary>
    /// Enrich an activity.
    /// </summary>
    /// <remarks>This method will only be called by SerilogTracing for activities that are expected to be enriched with data.
    /// This is, activities where <see cref="Activity.IsAllDataRequested"/> is true.</remarks>
    /// <param name="activity">The activity to enrich with instrumentation.</param>
    protected abstract void InstrumentActivity(Activity activity);
    
    /// <inheritdoc />
    bool IActivityInstrumentor.ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == Constants.SerilogTracingActivitySourceName;
    }
    
    /// <inheritdoc />
    void IActivityInstrumentor.InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
            case Constants.SerilogTracingActivityStartedEventName:
                if (!ShouldSubscribeTo(activity.Source.Name))
                    return;
                
                InstrumentActivity(activity);
                return;
        }
    }
}
