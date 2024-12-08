using System.Diagnostics;
using Xunit;

namespace SerilogTracing.Instrumentation.AspNetCore.Tests;

public class TraceParentHeaderTests
{
    [Theory]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", true, ActivityTraceFlags.Recorded)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00", true, ActivityTraceFlags.None)]
    [InlineData("notatraceparent", false, null)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-0x3", false, null)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-0-2", false, null)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331--2", false, null)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-02", true, (ActivityTraceFlags)0b10)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-03", true, ActivityTraceFlags.Recorded | (ActivityTraceFlags)0b10)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-ff", true, (ActivityTraceFlags)0xff)]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01-03", true, ActivityTraceFlags.Recorded)]
    public void FlagsAreParsed(string header, bool expectedResult, ActivityTraceFlags? expectedFlags)
    {
        Assert.Equal(expectedResult, TraceParentHeader.TryParse(header, out var flags));
        Assert.Equal(expectedFlags, flags);
    }
}