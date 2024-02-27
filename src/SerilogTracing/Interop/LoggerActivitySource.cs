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

namespace SerilogTracing.Interop;

static class LoggerActivitySource
{
    static ActivitySource Instance { get; } = new(Constants.SerilogTracingActivitySourceName, null);

    public static Activity? TryStartActivity(string name)
    {
        // `ActivityKind` might be passed through here in the future. The `Activity` constructor does
        // not accept this.

        if (Instance.HasListeners())
        {
            // Tracing is enabled; if this returns `null`, sampling is suppressing the activity and so therefore
            // should the logging layer.
            var listenerActivity = Instance.CreateActivity(name, ActivityKind.Internal);

            listenerActivity?.Start();

            return listenerActivity;
        }

        // Tracing is not enabled. Levels are everything, and the level check has already been performed by the
        // caller, so we're in business!

        var manualActivity = new Activity(name);
        if (Activity.Current is { } parent)
        {
            manualActivity.SetParentId(parent.TraceId, parent.SpanId, parent.ActivityTraceFlags);
        }
        else
        {
            manualActivity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        }

        manualActivity.Start();

        return manualActivity;
    }
}
