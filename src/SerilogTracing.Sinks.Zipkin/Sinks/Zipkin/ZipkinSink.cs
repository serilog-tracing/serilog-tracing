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

    public ZipkinSink(Uri endpoint, HttpMessageHandler messageHandler)
    {
        _client = new HttpClient(messageHandler) { BaseAddress = endpoint };
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var content = ZipkinBodyFormatter.FormatRequestContent(batch);
        if (content == null)
            return;

        var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/spans")
        {
            Content = new StringContent(content, _encoding, "application/json")
        };

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }
}