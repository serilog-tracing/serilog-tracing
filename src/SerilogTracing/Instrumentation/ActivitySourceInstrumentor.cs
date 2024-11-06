using System.Diagnostics;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// An instrumentor that observes events when activities are started or stopped.
/// </summary>
class ActivitySourceInstrumentor : IActivityInstrumentor
{
    public ActivitySourceInstrumentor(
        Func<ActivitySource, bool> shouldInstrumentActivitySource,
        Action<Activity>? onActivityStarted,
        Action<Activity>? onActivityStopped
    )
    {
        _shouldInstrumentActivitySource = shouldInstrumentActivitySource;
        _onActivityStarted = onActivityStarted;
        _onActivityStopped = onActivityStopped;
    }

    readonly Func<ActivitySource, bool> _shouldInstrumentActivitySource;
    readonly Action<Activity>? _onActivityStarted;
    readonly Action<Activity>? _onActivityStopped;

    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == Constants.SerilogTracingActivitySourceName;
    }

    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        if (!_shouldInstrumentActivitySource(activity.Source))
            return;
        
        switch (eventName)
        {
            case Constants.SerilogTracingActivityStartedEventName:
                _onActivityStarted?.Invoke(activity);
                return;
            case Constants.SerilogTracingActivityStoppedEventName:
                _onActivityStopped?.Invoke(activity);
                return;
        }
    }
}
