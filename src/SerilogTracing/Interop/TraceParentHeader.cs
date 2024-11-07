using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SerilogTracing.Interop;

static class TraceParentHeader
{
    internal static bool TryParse(string traceParentHeaderValue, [NotNullWhen(true)] out ActivityTraceFlags? flags)
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
