using System.Diagnostics;
using Xunit;

namespace SerilogTracing.Instrumentation.AspNetCore.Tests;

public class TraceParentHeaderTests
{
    [Theory]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", true, ActivityTraceFlags.Recorded)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00", true, ActivityTraceFlags.None)]
    [InlineData("notatraceparent", false, null)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-02", false, null)]
    public void FlagsAreParsed(string header, bool expectedResult, ActivityTraceFlags? expectedFlags)
    {
        Assert.Equal(expectedResult, TraceParentHeader.TryParse(header, out var flags));
        Assert.Equal(expectedFlags, flags);
    }
}