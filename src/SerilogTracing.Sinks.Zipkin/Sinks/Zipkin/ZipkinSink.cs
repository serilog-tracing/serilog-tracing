using System.Text;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Serilog.Templates;
using SerilogTracing.Core;
using SerilogTracing.Expressions;

namespace SerilogTracing.Sinks.Zipkin;

class ZipkinSink : IBatchedLogEventSink
{
    readonly Encoding _encoding = new UTF8Encoding(false);
    readonly HttpClient _client;
    readonly ExpressionTemplate _formatter = new("""
    {
        {
            id: @sp,
            traceId: @tr,
            parentId: ParentSpanId,
            name: @m,
            timestamp: Microseconds(FromUnixEpoch(SpanStartTimestamp)),
            duration: Microseconds(Elapsed()),
            kind: ToUpperInvariant(SpanKind),
            localEndpoint: {serviceName: Application},
            tags: AsStringTags(rest())
        }
    }
    """, nameResolver: new ZipkinNameResolver());

    public ZipkinSink(Uri endpoint, HttpMessageHandler messageHandler)
    {
        _client = new HttpClient(messageHandler) { BaseAddress = endpoint };
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var content = FormatRequestContent(batch);
        if (content == null)
            return;

        var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/spans")
        {
            Content = new StringContent(content, _encoding, "application/json")
        };

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    string? FormatRequestContent(IEnumerable<LogEvent> batch)
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
            _formatter.Format(logEvent, content);
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

    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }
}