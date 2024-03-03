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
using System.Reflection;
using Serilog.Expressions;

namespace SerilogTracing.Expressions;

/// <summary>
/// Adds expression support for <c>TimeSpan Elapsed()</c>, <c>bool IsSpan()</c>, <c>bool IsRootSpan()</c>,
/// <c>TimeSpan FromUnixEpoch(DateTime)</c>, <c>long Milliseconds(TimeSpan)</c>, <c>long Microseconds(TimeSpan)</c>,
/// <c>ulong or long Nanoseconds(TimeSpan)</c>. Note that the <c>Nanoseconds</c> function is undefined on overflow or
/// underflow.
/// </summary>
public class TracingNameResolver : NameResolver
{
    readonly NameResolver _tracingFunctions = new StaticMemberNameResolver(typeof(TracingFunctions));

    /// <inheritdoc/>
    public override bool TryResolveFunctionName(string name, [NotNullWhen(true)] out MethodInfo? implementation)
    {
        return _tracingFunctions.TryResolveFunctionName(name, out implementation);
    }
}
