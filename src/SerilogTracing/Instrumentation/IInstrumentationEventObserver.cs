using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// Apply instrumentation directly without regard for the current activity.
/// </summary>
public interface IInstrumentationEventObserver
{
    /// <summary>
    /// Apply instrumentation with context from a diagnostic event. This interface enables
    /// instrumentors to handle the raw diagnostic event without regard to the state of
    /// <see cref="Activity.Current"/>.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="eventArgs">The value of the event.</param>
    void OnNext(string eventName, object? eventArgs);
}