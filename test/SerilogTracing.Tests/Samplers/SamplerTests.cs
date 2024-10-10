using System.Diagnostics;
using Xunit;

namespace SerilogTracing.Tests.Samplers;

public abstract class SamplerTests
{
    protected static ActivitySamplingResult GetSamplingDecision(SampleActivity<ActivityContext> sampler, ActivityKind kind, ActivityContext parentContext)
    {
        using var source = new ActivitySource(Guid.NewGuid().ToString("n"));

        using var listener = new ActivityListener();
        
        // ReSharper disable once AccessToDisposedClosure
        listener.ShouldListenTo = s => s == source;
        
        ActivitySamplingResult? decision = null;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
        {
            var actual = sampler(ref options);
            decision = actual;
            return actual;
        };
        
        ActivitySource.AddActivityListener(listener);

        source.CreateActivity(Guid.NewGuid().ToString("n"), kind, parentContext);
        
        Assert.NotNull(decision);
        
        return decision.Value;
    }
}