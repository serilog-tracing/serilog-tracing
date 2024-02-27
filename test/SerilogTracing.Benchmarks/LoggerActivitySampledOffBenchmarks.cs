using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Serilog;
using Serilog.Events;

namespace SerilogTracing.Benchmarks;

/// <summary>
/// These benchmarks cover usage when the intended span is fully captured, in an application that uses full-featured
/// tracing.
/// </summary>
[MemoryDiagnoser]
public class LoggerActivitySampledOffBenchmarks: IDisposable
{
    readonly ILogger _log = new LoggerConfiguration().MinimumLevel.Is(LevelAlias.Minimum).CreateLogger();
    readonly Exception _exception = new();
    readonly IDisposable _activityListener;

    public LoggerActivitySampledOffBenchmarks()
    {
        _activityListener = new ActivityListenerConfiguration()
            .Instrument.WithDefaultInstrumentation(false)
            .Sample.Using((ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None)
            .TraceTo(_log);
    }

    [Benchmark]
    public LoggerActivity StartThenDispose()
    {
        var activity = _log.StartActivity("Benchmark");
        activity.Dispose();
        return activity;
    }
    
    [Benchmark]
    public LoggerActivity StartThenComplete()
    {
        var activity = _log.StartActivity("Benchmark");
        activity.Complete(LogEventLevel.Error, _exception);
        return activity;
    }

    public void Dispose()
    {
        _activityListener.Dispose();
    }
}
