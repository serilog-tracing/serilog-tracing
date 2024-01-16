using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class DiagnosticEventObserver: IObserver<KeyValuePair<string,object?>>
{
    internal DiagnosticEventObserver(IActivityInstrumentor instrumentor)
    {
        _instrumentor = instrumentor;
    }
    
    IActivityInstrumentor _instrumentor;
    
    public void OnCompleted()
    {
        
    }

    public void OnError(Exception error)
    {
        
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Value == null || Activity.Current == null) return;
        var activity = Activity.Current;
        
        _instrumentor.InstrumentActivity(activity, value.Key, value.Value);
    }
}