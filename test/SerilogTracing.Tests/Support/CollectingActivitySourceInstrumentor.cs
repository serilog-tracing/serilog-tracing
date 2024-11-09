using System.Diagnostics;
using SerilogTracing.Instrumentation;

namespace SerilogTracing.Tests.Support;

class CollectingActivitySourceInstrumentor : ActivitySourceInstrumentor
{
    public Activity? StartedActivity { get; set; }
    public Activity? StoppedActivity { get; set; }

    public override bool ShouldSubscribeTo(string activitySourceName)
    {
        return true;
    }

    public override void InstrumentOnActivityStarted(Activity activity)
    {
        StartedActivity = activity;
    }
    
    public override void InstrumentOnActivityStopped(Activity activity)
    {
        StoppedActivity = activity;
    }
}