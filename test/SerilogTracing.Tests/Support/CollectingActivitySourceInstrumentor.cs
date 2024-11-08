using System.Diagnostics;
using SerilogTracing.Instrumentation;

namespace SerilogTracing.Tests.Support;

class CollectingActivitySourceInstrumentor : ActivitySourceInstrumentor
{
    public Activity? Activity { get; set; }

    protected override bool ShouldSubscribeTo(string activitySourceName)
    {
        return true;
    }

    protected override void InstrumentActivity(Activity activity)
    {
        Activity = activity;
    }
}