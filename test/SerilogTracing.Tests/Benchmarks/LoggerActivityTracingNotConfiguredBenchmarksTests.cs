using SerilogTracing.Benchmarks;
using Xunit;

namespace SerilogTracing.Tests.Benchmarks;

[Collection("Shared")]
public class LoggerActivityTracingNotConfiguredBenchmarksTests
{
    [Fact]
    public void TracingNotConfiguredStartThenDispose()
    {
        var benchmarks = new LoggerActivityTracingNotConfiguredBenchmarks();
        var activity = benchmarks.StartThenDispose();
        Assert.NotNull(activity);
        Assert.NotSame(LoggerActivity.None, activity);
        Assert.NotNull(activity.Activity);
        Assert.Equal("", activity.Activity.Source.Name);
    }

    [Fact]
    public void TracingNotConfiguredStartThenComplete()
    {
        var benchmarks = new LoggerActivityTracingNotConfiguredBenchmarks();
        var activity = benchmarks.StartThenComplete();
        Assert.NotNull(activity);
        Assert.NotSame(LoggerActivity.None, activity);
        Assert.NotNull(activity.Activity);
        Assert.Equal("", activity.Activity.Source.Name);
    }
}
