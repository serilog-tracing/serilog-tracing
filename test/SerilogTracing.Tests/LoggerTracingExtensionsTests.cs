using System.Diagnostics;
using Serilog;
using Serilog.Events;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests;

[Collection("Shared")]
public class LoggerTracingExtensionsTests
{
    static LoggerTracingExtensionsTests()
    {
        // This is necessary to force activity id allocation on .NET Framework and early .NET Core versions. When this isn't
        // done, log events end up carrying null trace and span ids (which won't work with SerilogTracing).
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
    }
    
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, null, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, null, false)]
    public void ActivityIsGeneratedWhen(bool levelEnabled, bool tracingEnabled, bool? includedInSample, bool activityExpected)
    {
        // This rather inelegantly tests the algorithm spanning LoggerTracingExtensions and SerilogActivitySource that
        // determines whether or not a LoggerActivity and corresponding Activity will be generated.
        //
        //   1. Is the default completion level (passed to `StartActivity()`) enabled?
        //     * If no, return `None`.
        //   2. Does the `"Serilog"` activity source have listeners? (I.e. has `EnableTracing()` been called, or is
        //      another library hooking in to record traces?)
        //     * If no, return a new `LoggerActivity` wrapping a new `Activity`.
        //   3. Does `SerilogActivitySource.Instance.StartActivity()` return an activity? (I.e. is not suppressed by sampling?)
        //     * If yes, return a new `LoggerActivity` wrapping it.
        //     * If no, return `None` .
        
        const string activitySourceContext = "SerilogTracing.Tests.LoggerTracingExtensionsTests";
        const LogEventLevel activityLevel = LogEventLevel.Information;
        
        var sink = new CollectingSink();
        var log = new LoggerConfiguration()
            .MinimumLevel.Override(activitySourceContext, levelEnabled ? activityLevel : LevelAlias.Off)
            .WriteTo.Sink(sink)
            .CreateLogger();

        var configuration = new ActivityListenerConfiguration();
        if (includedInSample is { } always)
        {
            var result = always ? ActivitySamplingResult.AllData : ActivitySamplingResult.None;
            configuration.Sample.Using((ref ActivityCreationOptions<ActivityContext> _) => result);
        }

        using var _ = tracingEnabled ? configuration.TraceTo(log) : null;
        
        // This activity source is "outside" SerilogTracing and only exists to 
        using var source = Some.ActivitySource();
        using var listener = Some.AlwaysOnListenerFor(source.Name);
        using var parent = source.StartActivity(Some.String());
        
        Assert.NotNull(parent);
        parent.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        
        Assert.NotEqual(default(ActivityTraceId).ToHexString(), parent.TraceId.ToHexString());
        Assert.NotEqual(default(ActivitySpanId).ToHexString(), parent.SpanId.ToHexString());
        
        var activity = log
            .ForContext(Serilog.Core.Constants.SourceContextPropertyName, activitySourceContext)
            .StartActivity(activityLevel, Some.String());
        
        activity.Complete();

        if (activityExpected)
        {
            Assert.NotSame(LoggerActivity.None, activity);
            
            var span = sink.SingleEvent;

            Assert.Equal(parent.TraceId, span.TraceId);
            Assert.Equal(parent.SpanId,
                (span.Properties[Core.Constants.ParentSpanIdPropertyName] as ScalarValue)?.Value);
        }
        else
        {
            Assert.Same(LoggerActivity.None, activity);
            Assert.Empty(sink.Events);
        }
    }
}
