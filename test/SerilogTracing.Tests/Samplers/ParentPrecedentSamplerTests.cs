using System.Diagnostics;
using SerilogTracing.Samplers;
using Xunit;

namespace SerilogTracing.Tests.Samplers;

public class ParentPrecedentSamplerTests: SamplerTests
{
    [Theory]
    [InlineData(false, false, false, null)]
    [InlineData(true, false, false, ActivitySamplingResult.None)]
    [InlineData(true, true, false, ActivitySamplingResult.PropagationData)]
    [InlineData(true, false, true, ActivitySamplingResult.AllDataAndRecorded)]
    [InlineData(true, true, true, ActivitySamplingResult.AllDataAndRecorded)]
    public void SamplingDecisionAlwaysFollowsParent(bool hasParent, bool parentIsRemote, bool parentIsRecorded, ActivitySamplingResult? expected)
    {
        var parentContext = hasParent
            ? new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
                parentIsRecorded ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
                traceState: null,
                isRemote: parentIsRemote)
            : default;

        var deferredToNested = false;
        var sampler = ParentPrecedentSampler.Create((ref ActivityCreationOptions<ActivityContext> options) =>
        {
            deferredToNested = true;
            return ActivitySamplingResult.None;
        });
        
        var decision = GetSamplingDecision(sampler, ActivityKind.Internal, parentContext);

        if (expected is { } fromParent)
        {
            Assert.Equal(fromParent, decision);
            Assert.False(deferredToNested);
        }
        else
        {
            Assert.True(deferredToNested);
        }
    }
}
