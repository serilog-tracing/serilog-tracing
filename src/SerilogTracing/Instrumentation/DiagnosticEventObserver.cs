using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class DiagnosticEventObserver: IObserver<KeyValuePair<string,object?>>
{
    readonly IActivityInstrumentor _instrumentor;
    
    internal DiagnosticEventObserver(IActivityInstrumentor instrumentor)
    {
        _instrumentor = instrumentor;
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Value == null || Activity.Current == null) return;

        OnNext(Activity.Current, value.Key, value.Value);
    }
    
    internal void OnNext(Activity activity, string eventName, object eventValue)
    {
        if (!ActivityInstrumentation.IsDataSuppressed(activity))
        {
            _instrumentor.InstrumentActivity(activity, eventName, eventValue);
        }
    }
}