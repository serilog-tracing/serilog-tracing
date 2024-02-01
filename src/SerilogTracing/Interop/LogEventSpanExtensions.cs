using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using SerilogTracing.Core;

namespace SerilogTracing.Interop;

static class LogEventSpanExtensions
{
    internal static bool TryGetElapsed(this LogEvent logEvent, [NotNullWhen(true)] out TimeSpan? elapsed)
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