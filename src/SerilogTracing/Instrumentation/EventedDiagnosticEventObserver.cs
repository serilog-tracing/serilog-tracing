using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

// This alternative implementation to DiagnosticEventObserver avoids an extra downcast or flow control in that
// type, for instrumentors that require raw events.
sealed class EventedDiagnosticEventObserver: IObserver<KeyValuePair<string,object?>>
{
    readonly IActivityInstrumentor _instrumentor;
    readonly IInstrumentationEventObserver _observer;

    internal EventedDiagnosticEventObserver(IActivityInstrumentor instrumentor, IInstrumentationEventObserver observer)
    {
        _instrumentor = instrumentor;
        _observer = observer;
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        _observer.OnNext(value.Key, value.Value);
        
        var activity = Activity.Current;
        if (value.Value == null || activity == null) return;
        
        _instrumentor.InstrumentActivity(activity, value.Key, value.Value);
    }
}