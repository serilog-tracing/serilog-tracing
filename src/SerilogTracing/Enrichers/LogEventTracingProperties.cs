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

using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using SerilogTracing.Core;

namespace SerilogTracing.Enrichers;

static class LogEventTracingProperties
{
    public static bool TryGetElapsed(LogEvent logEvent, [NotNullWhen(true)] out TimeSpan? elapsed)
    {
        if (!logEvent.Properties.TryGetValue(Constants.SpanStartTimestampPropertyName, out var st) ||
            st is not ScalarValue
            {
                Value: DateTime spanStart
            })
        {
            elapsed = null;
            return false;
        }

        elapsed = logEvent.Timestamp - spanStart;
        return true;
    }
}