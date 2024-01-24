using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class DiagnosticListenerObserver : IObserver<DiagnosticListener>, IDisposable
{
    internal DiagnosticListenerObserver(IReadOnlyList<IActivityInstrumentor> instrumentors)
    {
        _instrumentors = instrumentors;
    }

    readonly IReadOnlyList<IActivityInstrumentor> _instrumentors;
    readonly List<IDisposable?> _subscriptions = [];
    
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(DiagnosticListener value)
    {
        foreach (var instrumentor in _instrumentors)
        {
            if (instrumentor.ShouldSubscribeTo(value.Name))
            {
                _subscriptions.Add(value.Subscribe(new DiagnosticEventObserver(instrumentor)));
            }
        }
    }

    public void Dispose()
    {
        var failedDisposes = new List<Exception>();
        
        foreach (var subscription in _subscriptions)
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
