﻿using SerilogTracing.Instrumentation;
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

    [Fact]
    public void ActivitySourceInstrumentorDoesNotSeeSuppressedActivities()
    {
        using var activity = Some.Activity();
        activity.IsAllDataRequested = false;

        var instrumentor = new CollectingActivitySourceInstrumentor();

        new DiagnosticEventObserver(instrumentor).OnNext(activity, "ActivityStarted", activity);

        Assert.Null(instrumentor.StartedActivity);
        Assert.Null(instrumentor.StoppedActivity);
    }

    [Fact]
    public void ActivitySourceInstrumentorSeesUnsuppressedActivities()
    {
        using var activity = Some.Activity();
        activity.IsAllDataRequested = true;

        var instrumentor = new CollectingActivitySourceInstrumentor();

        new DiagnosticEventObserver(instrumentor).OnNext(activity, "ActivityStarted", activity);

        Assert.Equal(activity, instrumentor.StartedActivity);
        Assert.Null(instrumentor.StoppedActivity);
        
        new DiagnosticEventObserver(instrumentor).OnNext(activity, "ActivityStopped", activity);
        
        Assert.Equal(activity, instrumentor.StoppedActivity);
    }
}