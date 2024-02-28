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
public class LoggerActivityTracingOnBenchmarks: IDisposable
{
    readonly ILogger _log = new LoggerConfiguration().CreateLogger();
    readonly Exception _exception = new();
    readonly IDisposable _loggerActivityListener;
    readonly ActivitySource _enabledSource = new(nameof(LoggerActivityTracingOnBenchmarks) + ".Included");
    readonly ActivitySource _ignoredSource = new(nameof(LoggerActivityTracingOnBenchmarks) + ".Ignored");
    readonly ActivityListener _ignoringActivityListener;

    public LoggerActivityTracingOnBenchmarks()
    {
        _loggerActivityListener = new ActivityListenerConfiguration()
            .InitialLevel.Override(_ignoredSource.Name, LogEventLevel.Verbose)
            .Instrument.WithDefaultInstrumentation(false)
            .TraceTo(_log, ignoreLevelChanges: true);

        _ignoringActivityListener = new ActivityListener();
        _ignoringActivityListener.ShouldListenTo = source => source == _ignoredSource;
        _ignoringActivityListener.Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(_ignoringActivityListener);
    }
    
    [Benchmark(Baseline = true)]
    public Activity? ActivitySourceBaseline()
    {
        var activity = _ignoredSource.StartActivity();
        activity?.Dispose();
        return activity;
    }

    [Benchmark]
    public Activity? ActivityListenerOnly()
    {
        var activity = _enabledSource.StartActivity();
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
        _loggerActivityListener.Dispose();
        _ignoredSource.Dispose();
        _enabledSource.Dispose();
        _ignoringActivityListener.Dispose();
    }
}
