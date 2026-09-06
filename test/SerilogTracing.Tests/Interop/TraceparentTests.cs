using System.Diagnostics;
using SerilogTracing.Interop;
using Xunit;

namespace SerilogTracing.Tests.Interop;

public class TraceparentTests
{
    public static TheoryData<string, ActivityTraceId, ActivitySpanId, ActivityTraceFlags> RoundtripCases()
    {
        return new()
        {
            { "00-00000000000000000000000000000000-0000000000000000-00", default, default, default },
            { "00-a3086082746a9521029abf706bb0b091-7267f2e822816a56-01", ActivityTraceId.CreateFromString("a3086082746a9521029abf706bb0b091"), ActivitySpanId.CreateFromString("7267f2e822816a56"), ActivityTraceFlags.Recorded },
            { "00-ffffffffffffffffffffffffffffffff-ffffffffffffffff-ff", ActivityTraceId.CreateFromString("ffffffffffffffffffffffffffffffff"), ActivitySpanId.CreateFromString("ffffffffffffffff"), (ActivityTraceFlags)255 },
        };
    }

    [Theory]
    [MemberData(nameof(RoundtripCases))]
    public void TraceparentFormats(string expected, ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags flags)
    {
        Assert.Equal(expected, new Traceparent(traceId, spanId, flags).ToString());
    }

    [Fact]
    public void TraceparentDefaultIsDefaultOfFields()
    {
        var defaultFields = new Traceparent(default, default, default);
        var defaultFull = (Traceparent)default;
        
        Assert.Equal(defaultFull.ToString(), defaultFields.ToString());
        Assert.Equal(defaultFull, defaultFields);
        Assert.Equal(defaultFull.GetHashCode(), defaultFields.GetHashCode());
        Assert.Equal(0, defaultFull.CompareTo(defaultFields));
    }

    [Fact]
    public void TraceparentCompares()
    {
        var a = new Traceparent(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);
        
        var b = new Traceparent(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);
        
        Assert.Equal(a, a);
        Assert.Equal(a.GetHashCode(), a.GetHashCode());
        Assert.Equal(0, a.CompareTo(a));
        
        Assert.NotEqual(a, b);
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(0, a.CompareTo(b));
    }
}