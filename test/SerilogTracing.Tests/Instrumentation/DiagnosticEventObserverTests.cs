using SerilogTracing.Instrumentation;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests.Instrumentation;

[Collection("Shared")]
public class DiagnosticEventObserverTests
{
    [Fact]
    public void ActivityInstrumentorDoesNotSeeSuppressedActivities()
    {
        using var activity = Some.Activity();
        activity.IsAllDataRequested = false;

        var instrumentor = new CollectingActivityInstrumentor();

        new DiagnosticEventObserver(instrumentor).OnNext(activity, "event", true);

        Assert.Null(instrumentor.Activity);
        Assert.Null(instrumentor.EventName);
        Assert.Null(instrumentor.EventArgs);
    }

    [Fact]
    public void ActivityInstrumentorSeesUnsuppressedActivities()
    {
        using var activity = Some.Activity();
        activity.IsAllDataRequested = true;

        var instrumentor = new CollectingActivityInstrumentor();
        
        new DiagnosticEventObserver(instrumentor).OnNext(activity, "event", true);
        
        Assert.Equal(activity, instrumentor.Activity);
        Assert.Equal("event", instrumentor.EventName);
        Assert.Equal(true, instrumentor.EventArgs);
    }
}