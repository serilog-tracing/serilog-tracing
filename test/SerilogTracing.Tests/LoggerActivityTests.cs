using System.Diagnostics;
using Serilog;
using Serilog.Events;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests;

[Collection("Shared")]
public class LoggerActivityTests
{
    [Theory]
    [InlineData(LogEventLevel.Debug, null, ActivityStatusCode.Ok, LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Debug, LogEventLevel.Debug, ActivityStatusCode.Ok, LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information, null, ActivityStatusCode.Ok, LogEventLevel.Information)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Warning, ActivityStatusCode.Ok, LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Debug, ActivityStatusCode.Ok, LogEventLevel.Information)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Error, ActivityStatusCode.Error, LogEventLevel.Error)]
    public void ExpectedCompletionLevelIsUsed(LogEventLevel initialLevel, LogEventLevel? completionLevel, ActivityStatusCode expectedStatusCode, LogEventLevel expectedLevel)
    {
        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var activity = Some.Activity();
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.Start();

        var loggerActivity = new LoggerActivity(logger, initialLevel, activity, MessageTemplate.Empty, []);
        loggerActivity.Complete(completionLevel);

        var span = sink.SingleEvent;
        Assert.Equal(expectedLevel, span.Level);
        Assert.Equal(expectedStatusCode, activity.Status);
    }

    [Fact]
    public void ActivityStatusIsLeftUnsetOnDispose()
    {
        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var activity = Some.Activity();
        activity.Start();

        var loggerActivity = new LoggerActivity(logger, LogEventLevel.Information, activity, MessageTemplate.Empty, []);
        loggerActivity.Dispose();

        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public void NullActivityCausesLogEventSuppression()
    {
        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        var loggerActivity = new LoggerActivity(logger, LogEventLevel.Information, null, MessageTemplate.Empty, []);
        loggerActivity.Complete(LogEventLevel.Information);

        Assert.Empty(sink.Events);
    }

    [Fact]
    public void IsAllDataRequestedFalseCausesEnrichmentSuppression()
    {
        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var activity = Some.Activity();
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.IsAllDataRequested = false;
        activity.Start();

        var loggerActivity = new LoggerActivity(logger, LogEventLevel.Information, activity, MessageTemplate.Empty, []);

        loggerActivity.AddProperty("Discarded", true);

        loggerActivity.Complete(LogEventLevel.Information);

        var span = sink.SingleEvent;
        Assert.DoesNotContain("Discarded", span.Properties);
    }
}