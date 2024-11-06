using System.Diagnostics;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Instrumentation.AspNetCore.Tests;

public class HttpRequestInActivityInstrumentorTests
{
    static Activity CreateIncoming(ActivityTraceFlags parentFlags = ActivityTraceFlags.None)
    {
        var parent = new Activity(Some.String());
        parent.ActivityTraceFlags = parentFlags;
        parent.Start();
        var incoming = new Activity(Some.String());
        // ReSharper disable once RedundantArgumentDefaultValue
        incoming.SetParentId(parent.TraceId, parent.SpanId, ActivityTraceFlags.None);
        incoming.SetBaggage(Some.String(), Some.String());
        incoming.SetTag(Some.String(), Some.String());
        parent.Stop();
        return incoming;
    }
}