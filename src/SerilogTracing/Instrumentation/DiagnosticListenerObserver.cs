using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class DiagnosticListenerObserver : IObserver<DiagnosticListener>, IDisposable
{
    internal DiagnosticListenerObserver(IReadOnlyList<IActivityEnricher> enrichers)
    {
        _enrichers = enrichers;
    }

    IReadOnlyList<IActivityEnricher> _enrichers;
    
    List<IDisposable?> _subscription = new();
    
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(DiagnosticListener value)
    {
        foreach (var enricher in _enrichers)
        {
            if (enricher.SubscribeTo(value.Name))
            {
                _subscription.Add(value.Subscribe(new ActivityEnrichmentDiagnosticObserver(enricher)));
            }
        }
    }

    public void Dispose()
    {
        var failedDisposes = new List<Exception>();
        
        foreach (var subscription in _subscription)
        {
            try
            {
                subscription?.Dispose();
            }
            catch (Exception e)
            {
                failedDisposes.Add(e);
            }
        }

        if (failedDisposes.Count > 0)
        {
            throw new AggregateException(failedDisposes);
        }
    }
}
