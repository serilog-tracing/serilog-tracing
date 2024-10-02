using System.Diagnostics;
using Serilog;
using SerilogTracing;
using SerilogTracing.Configuration;
using SerilogTracing.Expressions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(Formatters.CreateConsoleTextFormatter())
    .CreateLogger();

using var _ = new ActivityListenerConfiguration()
    .Sample.Using(IntervalSampler.Create(7))
    .TraceToSharedLogger();

for (var i = 0; i < 10000; ++i)
{
    using var outer = Log.Logger.StartActivity("Outer {i}", i);
    using var inner = Log.Logger.StartActivity("Inner {i}", i);
    await Task.Delay(100);
}

static class IntervalSampler
{
    /// <summary>
    /// Record one trace in every <paramref name="interval"/> possible traces.
    /// </summary>
    /// <param name="interval">The sampling interval. Note that this is per root activity, not per individual activity.</param>
    /// <returns>A sampling function that can be provided to <see cref="ActivityListenerSamplingConfiguration.Using"/>.</returns>
    public static SampleActivity<ActivityContext> Create(ulong interval)
    {
        ArgumentOutOfRangeException.ThrowIfZero(interval);
        var next = 0ul;
        
        return (ref ActivityCreationOptions<ActivityContext> options) =>
        {
            // The string comparison is required here because of some inconsistencies in how empty/default span ids
            // are constructed.
            if (options.Parent.SpanId.ToHexString() != default(ActivitySpanId).ToHexString())
            {
                // The activity is a child of another; if the parent is recorded, the child is recorded. Otherwise,
                // there's no need to generate an activity at all.
                return (options.Parent.TraceFlags & ActivityTraceFlags.Recorded) == ActivityTraceFlags.Recorded ?
                    ActivitySamplingResult.AllDataAndRecorded :
                    ActivitySamplingResult.None;
            }

            // We're at the root; if the trace is not included in the sample, return `PropagationData` so that
            // we apply the same decision to child activities via the path above.
            var n = Interlocked.Increment(ref next) % interval - 1;
            return n == 0
                ? ActivitySamplingResult.AllDataAndRecorded
                : ActivitySamplingResult.PropagationData;
        };
    }
}