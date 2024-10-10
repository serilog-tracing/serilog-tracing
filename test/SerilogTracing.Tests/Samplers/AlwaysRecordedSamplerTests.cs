using System.Diagnostics;
using SerilogTracing.Samplers;
using Xunit;

namespace SerilogTracing.Tests.Samplers;

public class AlwaysRecordedSamplerTests: SamplerTests
{
    [Theory]
    [InlineData(false, false, false, ActivitySamplingResult.AllDataAndRecorded)]
    [InlineData(true, false, false, ActivitySamplingResult.AllDataAndRecorded)]
    [InlineData(true, true, false, ActivitySamplingResult.AllDataAndRecorded)]
    [InlineData(true, false, true, ActivitySamplingResult.AllDataAndRecorded)]
    [InlineData(true, true, true, ActivitySamplingResult.AllDataAndRecorded)]
    public void SamplingDecisionIsAlwaysRecorded(bool hasParent, bool parentIsRemote, bool parentIsRecorded, ActivitySamplingResult expected)
    {
        var parentContext = hasParent
            ? new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
                parentIsRecorded ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
                traceState: null,
                isRemote: parentIsRemote)
            : default;

        var sampler = AlwaysRecordedSampler.Create();
        
        var decision = GetSamplingDecision(sampler, ActivityKind.Internal, parentContext);

        Assert.Equal(expected, decision);
    }
}