using System.Diagnostics;
using SerilogTracing.Samplers;
using Xunit;

namespace SerilogTracing.Tests.Samplers;

public class IntervalSamplerTests: SamplerTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void OneTraceInNIsRecorded(bool hasParent, bool parentIsRemote, bool parentIsRecorded)
    {
        var parentContext = hasParent
            ? new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
                parentIsRecorded ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
                traceState: null,
                isRemote: parentIsRemote)
            : default;

        var sampler = IntervalSampler.Create(7);
        var recordedCount = 0;
        for (var i = 0; i < 77; ++i)
        {
            var result = GetSamplingDecision(sampler, ActivityKind.Internal, parentContext);
            if (result == ActivitySamplingResult.AllDataAndRecorded)
            {
                recordedCount += 1;
            }
            else
            {
                Assert.Equal(ActivitySamplingResult.PropagationData, result);
            }
        }
        
        Assert.Equal(11, recordedCount);
    }
}