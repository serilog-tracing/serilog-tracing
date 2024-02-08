using SerilogTracing.Instrumentation;

namespace SerilogTracing.Tests.Support;

class CollectingInstrumentationEventObserver : IInstrumentationEventObserver
{
    public void OnNext(string eventName, object? eventArgs)
    {
        EventName = eventName;
        EventArgs = eventArgs;
    }

    public string? EventName { get; set; }
    public object? EventArgs { get; set; }
}