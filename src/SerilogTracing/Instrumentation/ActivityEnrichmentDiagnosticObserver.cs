using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class ActivityEnrichmentDiagnosticObserver: IObserver<KeyValuePair<string,object?>>
{
    internal ActivityEnrichmentDiagnosticObserver(IActivityEnricher enricher)
    {
        _enricher = enricher;
    }
    
    IActivityEnricher _enricher;
    
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
        
        _enricher.EnrichActivity(activity, value.Key, value.Value);
    }
}