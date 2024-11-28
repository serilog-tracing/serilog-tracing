using Serilog.Events;
using Serilog.Templates;
using SerilogTracing.Core;

namespace SerilogTracing.Sinks.Zipkin;

static class ZipkinBodyFormatter
{
    static readonly ExpressionTemplate Formatter = new("""
    {
        {
            id: @sp,
            traceId: @tr,
            parentId: ParentSpanId,
            name: @m,
            timestamp: Round(Microseconds(FromUnixEpoch(SpanStartTimestamp)), 0),
            duration: Round(Microseconds(Elapsed()), 0),
            kind: ToUpperInvariant(SpanKind),
            localEndpoint: {serviceName: Application},
            tags: AsStringTags(rest())
        }
    }
    """, nameResolver: new ZipkinNameResolver());
    
    internal static string? FormatRequestContent(IEnumerable<LogEvent> batch)
    {
        var content = new StringWriter();
        content.Write('[');

        var any = false;
        foreach (var logEvent in batch.Where(IsSpan))
        {
            if (any)
            {
                content.Write(',');
            }
            else
            {
                any = true;
            }
            Formatter.Format(logEvent, content);
        }

        if (!any)
            return null;

        content.Write(']');
        return content.ToString();
    }
    
    static bool IsSpan(LogEvent logEvent)
    {
        return logEvent is { TraceId: not null, SpanId: not null } &&
               logEvent.Properties.TryGetValue(Constants.SpanStartTimestampPropertyName, out var sst) &&
               sst is ScalarValue { Value: DateTime };
    }
}