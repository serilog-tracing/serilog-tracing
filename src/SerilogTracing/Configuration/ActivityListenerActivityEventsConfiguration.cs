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
using Serilog.Events;
using SerilogTracing.Interop;

namespace SerilogTracing.Configuration;

/// <summary>
/// Controls configuration of embedded <see cref="ActivityEvent"/> handling.
/// </summary>
public class ActivityListenerActivityEventsConfiguration
{
    readonly ActivityListenerConfiguration _activityListenerConfiguration;
    ActivityEventRecording _activityEventRecording = ActivityEventRecording.None;

    internal ActivityListenerActivityEventsConfiguration(ActivityListenerConfiguration activityListenerConfiguration)
    {
        _activityListenerConfiguration = activityListenerConfiguration;
    }

    internal ActivityEventRecording Options => _activityEventRecording;

    /// <summary>
    /// Extract <see cref="ActivityEvent"/> objects from activities, and record these as regular log events.
    /// </summary>
    /// <remarks>Log events generated from activity events are emitted when the activity completes; this may cause
    /// their timestamps to appear out-of-order with respect to other log events. The first occurrence of any
    /// <c>exception</c> event will be mapped to the span's <see cref="LogEvent.Exception"/> and will not be emitted as
    /// a separate log event.</remarks>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration AsLogEvents()
    {
        _activityEventRecording = ActivityEventRecording.AsLogEvents;
        return _activityListenerConfiguration;
    }
}