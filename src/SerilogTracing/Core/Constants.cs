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

namespace SerilogTracing.Core;

/// <summary>
/// Constants used by SerilogTracing.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The name of the <see cref="ActivitySource"/> through which SerilogTracing publishes logger activities.
    /// </summary>
    public const string SerilogTracingActivitySourceName = "SerilogTracing";

    /// <summary>
    /// The name of the entry in <see cref="LogEvent.Properties"/> that carries a
    /// span's <see cref="System.Diagnostics.Activity.ParentId"/>, if there is one.
    /// </summary>
    public const string ParentSpanIdPropertyName = "ParentSpanId";

    /// <summary>
    /// The name of the entry in <see cref="LogEvent.Properties"/> that carries a
    /// span's start timestamp. All spans emitted by SerilogTracing carry this property.
    /// </summary>
    public const string SpanStartTimestampPropertyName = "SpanStartTimestamp";
    
    /// <summary>
    /// The name of the entry in <see cref="LogEvent.Properties"/> that carries a
    /// span's kind. The value will be an <see cref="ActivityKind"/>. The span kind is
    /// unset for <see cref="ActivityKind.Internal"/> spans: any span without an explicit
    /// kind should be assumed to be internal.
    /// </summary>
    public const string SpanKindPropertyName = "SpanKind";

    internal const string LogEventPropertyCollectionName = "SerilogTracing.LogEventPropertyCollection";
    internal const string SelfPropertyName = "SerilogTracing.LoggerActivity.Self";
    internal const string MessageTemplateOverridePropertyName = "SerilogTracing.LoggerActivity.MessageTemplate";

    internal const string ExceptionEventName = "exception";
}