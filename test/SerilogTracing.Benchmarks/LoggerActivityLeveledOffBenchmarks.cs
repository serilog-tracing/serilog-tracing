using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Serilog;
using Serilog.Events;

namespace SerilogTracing.Benchmarks;

/// <summary>
/// These benchmarks cover usage when the intended span is suppressed through logger levelling. The cost in this
/// case is the same regardless of whether and how tracing is configured.
/// </summary>
[MemoryDiagnoser]
public class LoggerActivityLeveledOffBenchmarks : IDisposable
{
    readonly ILogger _log = new LoggerConfiguration().MinimumLevel.Is(LevelAlias.Off).CreateLogger();
    readonly Exception _exception = new();
    readonly ActivitySource _activitySource = new(nameof(LoggerActivityLeveledOffBenchmarks));

    [Benchmark(Baseline = true)]
    public Activity? ActivitySourceBaseline()
    {
        var activity = _activitySource.StartActivity();
        activity?.Dispose();
        return activity;
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
        _activitySource.Dispose();
    }
}
