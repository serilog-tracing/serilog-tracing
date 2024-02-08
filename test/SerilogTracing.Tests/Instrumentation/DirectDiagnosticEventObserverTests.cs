using SerilogTracing.Instrumentation;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests.Instrumentation;

[Collection("Shared")]
public class DirectDiagnosticEventObserverTests
{
    [Fact]
    public void DiagnosticEventsAreForwardedToInnerObserver()
    {
        using var activity = Some.Activity();
        activity.IsAllDataRequested = true;
        activity.Start();

        var instrumentor = new CollectingActivityInstrumentor();
        var inner = new DiagnosticEventObserver(instrumentor);

        var directObserver = new CollectingInstrumentationEventObserver();

        var wrapper = new DirectDiagnosticEventObserver(inner, directObserver);
        wrapper.OnNext(new KeyValuePair<string, object?>("event", true));

        Assert.Equal("event", directObserver.EventName);
        Assert.Equal(true, directObserver.EventArgs);

        Assert.Equal(activity, instrumentor.Activity);
        Assert.Equal("event", instrumentor.EventName);
        Assert.Equal(true, instrumentor.EventArgs);
    }
}