using System.Diagnostics;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// 
/// </summary>
public abstract class ActivitySourceInstrumentor : IActivityInstrumentor
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="replacementActivitySourceName"></param>
    protected ActivitySourceInstrumentor(string replacementActivitySourceName)
    {
        ReplacementSource = new ReplacementActivitySource(replacementActivitySourceName);
    }

    /// <summary>
    /// 
    /// </summary>
    protected readonly ReplacementActivitySource ReplacementSource;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    protected abstract void InstrumentActivity(Activity activity);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    protected abstract bool ShouldInstrument(ActivitySource source);
    
    /// <inheritdoc />
    bool IActivityInstrumentor.ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == Constants.SerilogTracingActivitySourceName;
    }
    
    /// <inheritdoc />
    void IActivityInstrumentor.InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
            case Constants.SerilogTracingActivityStartedEventName:
                if (!ShouldInstrument(activity.Source) || !ReplacementSource.CanReplace(activity.Source))
                    return;
                
                InstrumentActivity(activity);
                return;
        }
    }
}
