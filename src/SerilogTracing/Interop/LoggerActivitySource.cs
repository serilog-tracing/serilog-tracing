using System.Diagnostics;

namespace SerilogTracing.Interop;

static class LoggerActivitySource
{
    const string Name = "Serilog";

    static ActivitySource Instance { get; } = new(Name, null);

    public static Activity? TryStartActivity(string name)
    {
        // `ActivityKind` might be passed through here in the future. The `Activity` constructor does
        // not accept this.
        
        if (Instance.HasListeners())
        {
            // Tracing is enabled; if this returns `null`, sampling is suppressing the activity and so therefore
            // should the logging layer.
            return Instance.StartActivity(name);
        }
        
        // Tracing is not enabled. Levels are everything, and the level check has already been performed by the
        // caller, so we're in business!

        var activity = new Activity(name);
        if (Activity.Current is {} parent)
        {
            activity.SetParentId(parent.TraceId, parent.SpanId, parent.ActivityTraceFlags);
        }

        activity.Start();

        return activity;
    }
}
