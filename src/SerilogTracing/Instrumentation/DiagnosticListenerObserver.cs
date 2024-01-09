using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class DiagnosticListenerObserver : IObserver<DiagnosticListener>, IDisposable
{
    IDisposable? _subscription;
    
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == "HttpHandlerDiagnosticListener")
        {
            _subscription = value.Subscribe(new HttpHandlerDiagnosticObserver());
        }
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
