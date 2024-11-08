using System.Diagnostics;
using SerilogTracing.Instrumentation;

namespace SerilogTracing.Tests.Support;

class CollectingActivitySourceInstrumentor : ActivitySourceInstrumentor
{
    public Activity? Activity { get; set; }

    public override bool ShouldSubscribeTo(string activitySourceName)
    {
        return true;
    }

    public override void InstrumentActivity(Activity activity)
    {
        Activity = activity;
    }
}