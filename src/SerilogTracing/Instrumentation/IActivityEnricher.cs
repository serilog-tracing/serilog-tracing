using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// 
/// </summary>
public interface IActivityEnricher
{
    /// <summary>
    /// Whether the enricher should subscribe to events from the given <see cref="DiagnosticListener"/>.
    /// </summary>
    /// <param name="listenerName">The <see cref="DiagnosticListener.Name"/> of the candidate <see cref="DiagnosticListener"/>.</param>
    /// <returns>Whether the enricher </returns>
    bool SubscribeTo(string listenerName);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="eventName"></param>
    /// <param name="eventArgs"></param>
    void EnrichActivity(Activity activity, string eventName, object eventArgs);
}
