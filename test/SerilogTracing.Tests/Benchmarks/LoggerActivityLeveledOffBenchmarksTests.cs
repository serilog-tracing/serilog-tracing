using SerilogTracing.Benchmarks;
using Xunit;

namespace SerilogTracing.Tests.Benchmarks;

[Collection("Shared")]
public class LoggerActivityLeveledOffBenchmarksTests
{
    [Fact]
    public void ActivitySourceBaseline()
    {
        var benchmarks = new LoggerActivityLeveledOffBenchmarks();
        var activity = benchmarks.ActivitySourceBaseline();
        Assert.Null(activity);
    }
    
    [Fact]
    public void LeveledOffStartThenDispose()
    {
        var benchmarks = new LoggerActivityLeveledOffBenchmarks();
        var activity = benchmarks.StartThenDispose();
        Assert.NotNull(activity);
        Assert.Same(LoggerActivity.None, activity);
        Assert.Null(activity.Activity);
    }
    
    [Fact]
    public void LeveledOffStartThenComplete()
    {
        var benchmarks = new LoggerActivityLeveledOffBenchmarks();
        var activity = benchmarks.StartThenComplete();
        Assert.NotNull(activity);
        Assert.Same(LoggerActivity.None, activity);
        Assert.Null(activity.Activity);
    }
}
