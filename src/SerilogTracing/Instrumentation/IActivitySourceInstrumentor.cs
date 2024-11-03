using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// 
/// </summary>
public interface IActivitySourceInstrumentor
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    void ActivityStarted(Activity activity);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    void ActivityStopped(Activity activity);
}
