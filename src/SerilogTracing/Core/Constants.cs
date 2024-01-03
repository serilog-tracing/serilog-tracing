using Serilog.Events;

namespace SerilogTracing.Core;

/// <summary>
/// Constants used by SerilogTracing.
/// </summary>
public static class Constants
{
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
}