using System.Diagnostics;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Parsing;
using SerilogTracing.Core;
using SerilogTracing.Expressions;
using Xunit;

namespace SerilogTracing.Tests;

public class TracingNameResolverTests
{
    public static object[][] Cases()
    {
        var start = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc);
        var end = start.AddMilliseconds(123).AddTicks(4567);
        var template = new MessageTemplate(new[] { new TextToken("") });
        var traceId = ActivityTraceId.CreateRandom();
        var rootSpanId = ActivitySpanId.CreateRandom();
        var childSpanId = ActivitySpanId.CreateRandom();
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var root = new LogEvent(end, LogEventLevel.Debug, null, template,
            [new LogEventProperty(Constants.SpanStartTimestampPropertyName, new ScalarValue(start))],
            traceId, rootSpanId);

        var child = new LogEvent(end, LogEventLevel.Debug, null, template,
            [
                new LogEventProperty(Constants.SpanStartTimestampPropertyName, new ScalarValue(start)),
                new LogEventProperty(Constants.ParentSpanIdPropertyName, new ScalarValue(rootSpanId)),
            ],
            traceId, childSpanId);

        var nonSpan = new LogEvent(end, LogEventLevel.Debug, null, template, []);

        return new object[][]
        {
            [root, "IsSpan()", true ],
            [child, "IsSpan()", true ],
            [nonSpan, "IsSpan()", false ],
            [root, "IsRootSpan()", true ],
            [child, "IsRootSpan()", false ],
            [nonSpan, "IsRootSpan()", false ],
            [root, "Elapsed()", end - start],
            [root, "Milliseconds(Elapsed())", 123.4567M],
            [root, "Microseconds(Elapsed())", 123456.7M],
            [root, "Nanoseconds(Elapsed())", 123456700UL],
            [root, "FromUnixEpoch(@t)", end - epoch],
            [root, "FromUnixEpoch(SpanStartTimestamp)", start - epoch]
        };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TracingFunctionsAreEvaluatedCorrectly(LogEvent logEvent, string expression, object? expected)
    {
        var expr = SerilogExpression.Compile(expression, nameResolver: new TracingNameResolver());
        var actual = expr(logEvent);
        if (expected == null)
            Assert.Null(actual);
        var scalar = Assert.IsType<ScalarValue>(actual);
        Assert.Equal(expected, scalar.Value);
    }
}