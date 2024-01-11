using System.Diagnostics;

namespace SerilogTracing.Interop;

static class LoggerActivitySource
{
    const string Name = "Serilog";

    static ActivitySource Instance { get; } = new(Name, null);

    public static Activity StartActivity(string name)
    {
        // `ActivityKind` might be passed through here in the future. The `Activity` constructor does
        // not accept this.
        
        // Sampling options are set by `ActivitySource.StartActivity()` and also need to be considered
        // here in the future.
        
        if (Instance.HasListeners())
        {
            return Instance.StartActivity(name) ??
                   throw new InvalidOperationException("`ActivitySource.StartActivity` unexpectedly returned `null`.");
        }

        var activity = new Activity(name);
        if (Activity.Current is {} parent)
        {
            activity.SetParentId(parent.TraceId, parent.SpanId, parent.ActivityTraceFlags);
        }

        activity.Start();

        return activity;
    }
}
