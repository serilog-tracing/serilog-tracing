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
/// Apply instrumentation directly without regard for the current activity.
/// </summary>
public interface IInstrumentationEventObserver
{
    /// <summary>
    /// Apply instrumentation with context from a diagnostic event. This interface enables
    /// instrumentors to handle the raw diagnostic event without regard to the state of
    /// <see cref="Activity.Current"/>.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="eventArgs">The value of the event.</param>
    void OnNext(string eventName, object? eventArgs);
}