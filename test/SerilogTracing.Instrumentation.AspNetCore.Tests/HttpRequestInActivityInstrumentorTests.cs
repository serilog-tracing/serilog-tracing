using System.Diagnostics;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Instrumentation.AspNetCore.Tests;

public class HttpRequestInActivityInstrumentorTests
{
    static Activity CreateIncoming()
    {
        var incoming = new Activity(Some.String());
        // ReSharper disable once RedundantArgumentDefaultValue
        incoming.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);
        incoming.SetBaggage(Some.String(), Some.String());
        incoming.SetTag(Some.String(), Some.String());
        return incoming;
    }

    [Theory]
    [InlineData(IncomingTraceParent.Trust)]
    [InlineData(IncomingTraceParent.Ignore)]
    [InlineData(IncomingTraceParent.Accept)]
    public void AllModesSucceedWithNoParent(IncomingTraceParent incomingTraceParent)
    {
        using var listener = Some.AlwaysOnListenerFor(HttpRequestInActivityInstrumentor.ReplacementActivitySourceName);
        var replacement = HttpRequestInActivityInstrumentor.CreateReplacementActivity(null, incomingTraceParent);
        Assert.NotNull(replacement);
        Assert.Equal(default, replacement.ParentSpanId);
        Assert.Equal(ActivityTraceFlags.Recorded, replacement.ActivityTraceFlags);
    }
    
    [Fact]
    public void NoDetailsAreInheritedInIgnoreMode()
    {
        using var listener = Some.AlwaysOnListenerFor(HttpRequestInActivityInstrumentor.ReplacementActivitySourceName);
        var incoming = CreateIncoming();
        var replacement = HttpRequestInActivityInstrumentor.CreateReplacementActivity(incoming, IncomingTraceParent.Ignore);
        Assert.NotNull(replacement);
        Assert.NotEqual(incoming.TraceId, replacement.TraceId);
        Assert.Equal(default, replacement.ParentSpanId);
        Assert.NotEqual(incoming.ActivityTraceFlags, replacement.ActivityTraceFlags);
        Assert.Empty(replacement.Baggage);
        Assert.Empty(replacement.TagObjects);
    }
    
    [Fact]
    public void LimitedDetailsAreInheritedInAcceptMode()
    {
        using var listener = Some.AlwaysOnListenerFor(HttpRequestInActivityInstrumentor.ReplacementActivitySourceName);
        var incoming = CreateIncoming();
        var replacement = HttpRequestInActivityInstrumentor.CreateReplacementActivity(incoming, IncomingTraceParent.Accept);
        Assert.NotNull(replacement);
        Assert.Equal(incoming.TraceId, replacement.TraceId);
        Assert.Equal(incoming.ParentSpanId, replacement.ParentSpanId);
        Assert.NotEqual(incoming.ActivityTraceFlags, replacement.ActivityTraceFlags);
        Assert.Empty(replacement.Baggage);
        Assert.NotEmpty(replacement.TagObjects);
    }
    
    [Fact]
    public void AllDetailsAreInheritedInTrustMode()
    {
        using var listener = Some.AlwaysOnListenerFor(HttpRequestInActivityInstrumentor.ReplacementActivitySourceName);
        var incoming = CreateIncoming();
        var replacement = HttpRequestInActivityInstrumentor.CreateReplacementActivity(incoming, IncomingTraceParent.Trust);
        Assert.NotNull(replacement);
        Assert.Equal(incoming.TraceId, replacement.TraceId);
        Assert.Equal(incoming.ParentSpanId, replacement.ParentSpanId);
        Assert.Equal(incoming.ActivityTraceFlags, replacement.ActivityTraceFlags);
        Assert.NotEmpty(replacement.Baggage);
        Assert.NotEmpty(replacement.TagObjects);
    }
}