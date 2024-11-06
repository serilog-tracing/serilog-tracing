using System.Diagnostics;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

class ReplacementActivitySource
{
    const string DefaultActivityName = "SerilogTracing.Instrumentation.ActivityInstrumentation.Activity";

    internal static ReplacementActivitySource Instance { get; } = new();
    
    readonly ActivitySource _source = new(Constants.ReplacementActivitySourceName);

    public Activity? CreateActivity(ActivityKind kind, ActivityContext context)
    {
        // Force initialization of the replacement listener instance
        _ = ReplacementActivityListener.Instance;
        
        var activity = _source.CreateActivity(DefaultActivityName, kind, context);

        return activity;
    }
}
