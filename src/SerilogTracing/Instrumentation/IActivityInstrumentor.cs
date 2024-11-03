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

namespace SerilogTracing.Instrumentation;

/// <summary>
/// Instrument <see cref="Activity">activities</see>.
/// </summary>
public interface IActivityInstrumentor
{
    /// <summary>
    /// Whether the instrumentor should subscribe to events from the given <see cref="DiagnosticListener"/>.
    /// </summary>
    /// <param name="diagnosticListenerName">The <see cref="DiagnosticListener.Name"/> of the candidate <see cref="DiagnosticListener"/>.</param>
    /// <returns>Whether the instrumentor should receive events from the given listener.</returns>
    bool ShouldSubscribeTo(string diagnosticListenerName);

    /// <summary>
    /// Enrich an activity with context from a diagnostic event.
    /// </summary>
    /// <remarks>This method will only be called by SerilogTracing for activities that are expected to be enriched with data.
    /// This is, activities where <see cref="Activity.IsAllDataRequested"/> is true.</remarks>
    /// <param name="activity">The activity to enrich with instrumentation.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="eventArgs">The value of the event.</param>
    void InstrumentActivity(Activity activity, string eventName, object eventArgs);
}