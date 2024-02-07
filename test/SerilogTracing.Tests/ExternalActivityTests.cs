using System.Diagnostics;
using Serilog;
using Serilog.Events;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests;

[Collection("Shared")]
public class ExternalActivityTests
{
    [Fact]
    public void ExternalActivitiesAreEmitted()
    {
        var source = new ActivitySource($"{typeof(ExternalActivityTests).FullName}.${nameof(ExternalActivitiesAreEmitted)}");
        
        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var _ = new TracingConfiguration().TraceTo(logger);

        using var activity = source.StartActivity()!;
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.Stop();
        
        Assert.Equal(LogEventLevel.Information, sink.SingleEvent.Level);
        Assert.Equal(activity.DisplayName, sink.SingleEvent.RenderMessage());
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

        using var _ = new TracingConfiguration()
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

        using var _ = new TracingConfiguration()
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

        using var _ = new TracingConfiguration()
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

        using var _ = new TracingConfiguration()
            .InitialLevel.Is(LogEventLevel.Debug)
            .TraceTo(logger);

        using var activity = source.StartActivity();
        Assert.Null(activity);
    }
}