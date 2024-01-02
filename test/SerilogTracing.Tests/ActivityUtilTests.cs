using System.Diagnostics;
using System.Runtime.ExceptionServices;
using SerilogTracing.Interop;
using Xunit;

namespace SerilogTracing.Tests;

public class ActivityUtilTests
{
    [Fact]
    public void ExceptionsRoundTripThroughEvents()
    {
        var activity = new Activity("Test");
        var exception = new DivideByZeroException();
        ExceptionDispatchInfo.SetCurrentStackTrace(exception);
        var expected = exception.ToString();
        
        activity.AddEvent(ActivityUtil.EventFromException(exception));
        var actual = ActivityUtil.ExceptionFromEvents(activity)?.ToString();
        
        Assert.Equal(expected, actual);
    }
}