using SerilogTracing.Benchmarks;
using Xunit;

namespace SerilogTracing.Tests.Benchmarks;

[Collection("Shared")]
public class LoggerActivitySampledOffBenchmarksTests
{
    [Fact]
    public void SampledOffStartThenDispose()
    {
        var benchmarks = new LoggerActivityLeveledOffBenchmarks();
        var activity = benchmarks.StartThenDispose();
        Assert.NotNull(activity);
        Assert.Same(LoggerActivity.None, activity);
        Assert.Null(activity.Activity);
    }
    
    [Fact]
    public void SampledOffStartThenComplete()
    {
        var benchmarks = new LoggerActivityLeveledOffBenchmarks();
        var activity = benchmarks.StartThenComplete();
        Assert.NotNull(activity);
        Assert.Same(LoggerActivity.None, activity);
        Assert.Null(activity.Activity);
    }
}
