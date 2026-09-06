using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using SerilogTracing.Interop;

namespace SerilogTracing.Benchmarks;

[MemoryDiagnoser]
public class TraceparentBenchmarks
{
    // The inputs are randomized on each run so that they differ between benchmark runs,
    // which thwarts branch prediction from skewing the measured numbers.
    public static IEnumerable<object[]> RandomInputs()
    {
        yield return new object[]
        {
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            (ActivityTraceFlags)new Random().Next(0, 4),
        };
    }

    [Benchmark]
    [ArgumentsSource(nameof(RandomInputs))]
    public string Format(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags flags)
        => new Traceparent(traceId, spanId, flags).ToString();
}