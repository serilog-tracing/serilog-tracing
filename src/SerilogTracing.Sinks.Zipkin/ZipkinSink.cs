using System.Text;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Sinks.PeriodicBatching;
using Serilog.Templates;
using SerilogTracing.Core;

namespace SerilogTracing.Sinks.Zipkin;

public class ZipkinSink: IBatchedLogEventSink
{
    readonly Encoding _encoding = new UTF8Encoding(false);
    readonly HttpClient _client;
    readonly ExpressionTemplate _formatter = new("""
    {
        {
            id: @sp,
            traceId: @tr,
            parentId: ParentSpanId,
            name: @mt,
            timestamp: Micros(SpanStartTimestamp),
            duration: Micros(@t) - Micros(SpanStartTimestamp),
            localEndpoint: {serviceName: Application},
            tags: rest()
        }
    }
    """, nameResolver: new StaticMemberNameResolver(typeof(Operators)));
    
    public ZipkinSink(Uri endpoint)
    {
        _client = new HttpClient { BaseAddress = endpoint };
    }
        
    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        // ReSharper disable MethodHasAsyncOverload
        
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
        {
            return;
        }
        
        content.Write(']');
        
        // ReSharper restore MethodHasAsyncOverload

        var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/spans")
        {
            Content = new StringContent(content.ToString(), _encoding, "application/json")
        };

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
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