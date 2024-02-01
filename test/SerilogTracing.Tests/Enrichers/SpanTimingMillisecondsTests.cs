using Serilog.Events;
using SerilogTracing.Enrichers;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests.Enrichers;

public class SpanTimingMillisecondsTests
{
    [Fact]
    void EnricherIsAppliedToSpans()
    {
        var start = DateTime.UtcNow;

        var logEvent = Some.SerilogEvent("Message", timestamp: start + TimeSpan.FromSeconds(5),
            properties: new LogEventProperty[] { new("SpanStartTimestamp", new ScalarValue(start)) });
        
        new SpanTimingMilliseconds("Elapsed").Enrich(logEvent, new ScalarLogEventPropertyFactory());
        
        Assert.Equal(5000D, ((ScalarValue)logEvent.Properties["Elapsed"]).Value);
        
        logEvent = Some.SerilogEvent("Message", timestamp: start + TimeSpan.FromSeconds(5));
        
        new SpanTimingMilliseconds("Elapsed").Enrich(logEvent, new ScalarLogEventPropertyFactory());
        
        Assert.False(logEvent.Properties.ContainsKey("Elapsed"));
    }
}