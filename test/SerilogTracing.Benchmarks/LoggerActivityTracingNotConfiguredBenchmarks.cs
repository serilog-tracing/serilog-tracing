using BenchmarkDotNet.Attributes;
using Serilog;
using Serilog.Events;

namespace SerilogTracing.Benchmarks;

/// <summary>
/// These benchmarks cover usage when tracing is not configured; activities are always created in these cases.
/// </summary>
[MemoryDiagnoser]
public class LoggerActivityTracingNotConfiguredBenchmarks
{
    readonly ILogger _log = new LoggerConfiguration().MinimumLevel.Is(LevelAlias.Minimum).CreateLogger();
    readonly Exception _exception = new();
    
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
}
