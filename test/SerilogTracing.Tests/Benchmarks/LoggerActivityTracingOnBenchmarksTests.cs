using SerilogTracing.Benchmarks;
using Xunit;

namespace SerilogTracing.Tests.Benchmarks;

[Collection("Shared")]
public class LoggerActivityTracingOnBenchmarksTests
{
    [Fact]
    public void ActivitySourceBaseline()
    {
        var benchmarks = new LoggerActivityTracingOnBenchmarks();
        var activity = benchmarks.ActivitySourceBaseline();
        Assert.NotNull(activity);
    }

    [Fact]
    public void TracingOnStartThenDispose()
    {
        var benchmarks = new LoggerActivityTracingOnBenchmarks();
        var activity = benchmarks.StartThenDispose();
        Assert.NotNull(activity);
        Assert.NotSame(LoggerActivity.None, activity);
        Assert.NotNull(activity.Activity);
        Assert.Equal(Core.Constants.SerilogTracingActivitySourceName, activity.Activity.Source.Name);
    }

    [Fact]
    public void TracingOnStartThenComplete()
    {
        var benchmarks = new LoggerActivityTracingOnBenchmarks();
        var activity = benchmarks.StartThenComplete();
        Assert.NotNull(activity);
        Assert.NotSame(LoggerActivity.None, activity);
        Assert.NotNull(activity.Activity);
        Assert.Equal(Core.Constants.SerilogTracingActivitySourceName, activity.Activity.Source.Name);
    }
}
