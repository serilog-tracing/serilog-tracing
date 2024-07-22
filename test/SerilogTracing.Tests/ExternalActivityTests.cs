using System.Diagnostics;
using Serilog;
using Serilog.Events;
using SerilogTracing.Core;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests;

[Collection("Shared")]
public class ExternalActivityTests
{
    [Fact]
    public void ExternalActivitiesAreEmitted()
    {
        using var source = Some.ActivitySource();

        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var _ = new ActivityListenerConfiguration().TraceTo(logger);

        using var activity = source.StartActivity(ActivityKind.Client)!;
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.AddEvent(new("exception", tags: new ActivityTagsCollection([
            new("exception.message", "M"), 
            new("exception.stacktrace", "S"),
            new("property", "P")
        ])));
        activity.AddEvent(new("ignored"));
        activity.Stop();

        var span = sink.SingleEvent;
        Assert.Equal(LogEventLevel.Information, span.Level);
        Assert.Equal(activity.DisplayName, span.RenderMessage());
        Assert.Equal(ActivityKind.Client, ((ScalarValue)span.Properties[Constants.SpanKindPropertyName]).Value);
        Assert.Equal(activity.TraceId, span.TraceId);
        Assert.Equal(activity.SpanId, span.SpanId);
        Assert.NotNull(span.Exception);
        Assert.Equal("M", span.Exception.Message);
        Assert.Equal("S", span.Exception.ToString());
        Assert.False(span.Properties.ContainsKey("property"));
    }

    [Fact]
    public void EmbeddedEventsAreRecordedWhenConfigured()
    {
        using var source = Some.ActivitySource();

        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var _ = new ActivityListenerConfiguration()
            .ActivityEvents.AsLogEvents()
            .TraceTo(logger);

        using var activity = source.StartActivity(ActivityKind.Client)!;
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        
        // Mapped to LogEvent.Exception; tested previously
        activity.AddEvent(new("exception"));

        // First recorded event
        var timestamp = Some.Timestamp();
        activity.AddEvent(new("recorded", timestamp, new([new("property", "P")])));

        // Second recorded event
        activity.AddEvent(new("exception", tags: new([new("property", "Q")])));
        
        activity.Stop();
        
        Assert.Equal(3, sink.Events.Count);

        var recorded = sink.Events.First();
        Assert.Equal(timestamp, recorded.Timestamp);
        Assert.Equal("{ActivityEvent}", recorded.MessageTemplate.Text);
        Assert.Equal("recorded", ((ScalarValue)recorded.Properties["ActivityEvent"]).Value);
        Assert.Equal("P", ((ScalarValue)recorded.Properties["property"]).Value);
        Assert.Null(recorded.Exception);
        Assert.Equal(LogEventLevel.Information, recorded.Level);
        Assert.Equal(activity.TraceId, recorded.TraceId);
        Assert.Equal(activity.SpanId, recorded.SpanId);

        var exception = sink.Events.ElementAt(1);
        Assert.Equal("{ActivityEvent}", exception.MessageTemplate.Text);
        Assert.Equal("exception", ((ScalarValue)exception.Properties["ActivityEvent"]).Value);
        Assert.Equal("Q", ((ScalarValue)exception.Properties["property"]).Value);
        Assert.NotNull(exception.Exception);
        Assert.Equal(LogEventLevel.Information, exception.Level);
    }

    [Fact]
    public void ExternalActivitiesUseInitialLevel()
    {
        var source = new ActivitySource($"{typeof(ExternalActivityTests).FullName}.${nameof(ExternalActivitiesUseInitialLevel)}");

        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var _ = new ActivityListenerConfiguration()
            .InitialLevel.Is(LogEventLevel.Debug)
            .TraceTo(logger);

        using var activity = source.StartActivity()!;
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.Stop();

        Assert.Equal(LogEventLevel.Debug, sink.SingleEvent.Level);
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal, LogEventLevel.Fatal)]
    public void ErroredExternalActivitiesUseErrorLevel(LogEventLevel initialLevel, LogEventLevel completionLevel)
    {
        var source = new ActivitySource($"{typeof(ExternalActivityTests).FullName}.${nameof(ErroredExternalActivitiesUseErrorLevel)}.${initialLevel}.${completionLevel}");

        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var _ = new ActivityListenerConfiguration()
            .InitialLevel.Is(initialLevel)
            .TraceTo(logger);

        using var activity = source.StartActivity()!;
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.SetStatus(ActivityStatusCode.Error);
        activity.Stop();

        Assert.Equal(completionLevel, sink.SingleEvent.Level);
    }

    [Fact]
    public void ExternalActivitiesUseInitialLevelOverride()
    {
        var source = new ActivitySource($"{typeof(ExternalActivityTests).FullName}.${nameof(ExternalActivitiesUseInitialLevelOverride)}");

        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var _ = new ActivityListenerConfiguration()
            .InitialLevel.Override(typeof(ExternalActivityTests).FullName!, LogEventLevel.Debug)
            .TraceTo(logger);

        using var activity = source.StartActivity()!;
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.Stop();

        Assert.Equal(LogEventLevel.Debug, sink.SingleEvent.Level);
    }

    [Fact]
    public void ExternalActivitiesSampleInitialLevel()
    {
        using var source = Some.ActivitySource();

        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Warning)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var _ = new ActivityListenerConfiguration()
            .InitialLevel.Is(LogEventLevel.Debug)
            .TraceTo(logger);

        using var activity = source.StartActivity();
        Assert.Null(activity);
    }
}