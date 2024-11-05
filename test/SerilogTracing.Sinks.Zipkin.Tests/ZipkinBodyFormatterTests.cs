using System.Diagnostics;
using System.Text.Json;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Core;
using Xunit;

namespace SerilogTracing.Sinks.Zipkin.Tests;

public class ZipkinBodyFormatterTests
{
    static readonly MessageTemplateParser Parser = new();
    
    [Fact]
    public void FormatterGeneratesValidBody()
    {
        var start = DateTime.UnixEpoch + TimeSpan.FromMicroseconds(2.2);
        var end = start + TimeSpan.FromMicroseconds(2.2);

        var traceId = ActivityTraceId.CreateFromString("4bf92f3577b34da6a3ce929d0e0e4736");
        var spanId = ActivitySpanId.CreateFromString("00f067aa0ba902b7");
        var parentSpanId = ActivitySpanId.CreateFromString("f0130aba90726a07");
        
        var body = ZipkinBodyFormatter.FormatRequestContent([
            new LogEvent(
                end,
                LogEventLevel.Information,
                null,
                Parser.Parse("Hello, {User}"),
                [
                    new LogEventProperty("User", new ScalarValue("Zipkin")),
                    new LogEventProperty("Application", new ScalarValue("Test")),
                    new LogEventProperty(Constants.SpanStartTimestampPropertyName, new ScalarValue(start)),
                    new LogEventProperty(Constants.ParentSpanIdPropertyName, new ScalarValue(parentSpanId)),
                    new LogEventProperty(Constants.SpanKindPropertyName, new ScalarValue("Server"))
                ],
                traceId,
                spanId
            )
        ]);

        var expected = new object[]
        {
            new
            {
                id = "00f067aa0ba902b7",
                traceId = "4bf92f3577b34da6a3ce929d0e0e4736",
                parentId = "f0130aba90726a07",
                name = "Hello, Zipkin",
                timestamp = 2,
                duration = 2,
                kind = "SERVER",
                localEndpoint = new {
                    serviceName = "Test"
                },
                tags = new {
                    User = "Zipkin"
                }
            }
        };
        
        Assert.Equal(JsonSerializer.Serialize(expected), body);
    }
}