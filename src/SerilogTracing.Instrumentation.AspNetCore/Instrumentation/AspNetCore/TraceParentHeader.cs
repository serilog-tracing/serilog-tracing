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
using System.Diagnostics.CodeAnalysis;

namespace SerilogTracing.Instrumentation.AspNetCore;

static class TraceParentHeader
{
    public static bool TryParse(string traceParentHeaderValue, [NotNullWhen(true)] out ActivityTraceFlags? flags)
    {
        if (traceParentHeaderValue.EndsWith("-00"))
        {
            flags = ActivityTraceFlags.None;
            return true;
        }
        
        if (traceParentHeaderValue.EndsWith("-01"))
        {
            flags = ActivityTraceFlags.Recorded;
            return true;
        }

        flags = null;
        return false;
    }
}
