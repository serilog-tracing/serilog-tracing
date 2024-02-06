namespace SerilogTracing.Instrumentation;

// This alternative implementation to DiagnosticEventObserver avoids an extra downcast or flow control in that
// type, for instrumentors that require raw events.
sealed class DirectDiagnosticEventObserver: IObserver<KeyValuePair<string, object?>>
{
    readonly IObserver<KeyValuePair<string, object?>> _inner;
    readonly IInstrumentationEventObserver _observer;

    internal DirectDiagnosticEventObserver(IObserver<KeyValuePair<string, object?>> inner, IInstrumentationEventObserver observer)
    {
        _inner = inner;
        _observer = observer;
    }

    public void OnCompleted() => _inner.OnCompleted();

    public void OnError(Exception error) => _inner.OnError(error);

    public void OnNext(KeyValuePair<string, object?> value)
    {
        _observer.OnNext(value.Key, value.Value);
        _inner.OnNext(value);
    }
}
