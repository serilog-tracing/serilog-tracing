using System.Diagnostics;
using SerilogTracing.Instrumentation;

namespace SerilogTracing.Tests.Support;

class CollectingActivityInstrumentor : IActivityInstrumentor
{
    public Activity? Activity { get; set; }
    public string? EventName { get; set; }
    public object? EventArgs { get; set; }

    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return true;
    }

    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        Activity = activity;
        EventName = eventName;
        EventArgs = eventArgs;
    }
}