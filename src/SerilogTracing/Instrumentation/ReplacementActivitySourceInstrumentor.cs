using System.Diagnostics;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// 
/// </summary>
public abstract class ReplacementActivitySourceInstrumentor : IActivityInstrumentor
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="replacementActivitySourceName"></param>
    protected ReplacementActivitySourceInstrumentor(string replacementActivitySourceName)
    {
        _replacementSource = new ReplacementActivitySource(replacementActivitySourceName);
    }

    readonly ReplacementActivitySource _replacementSource;
    
    /// <inheritdoc />
    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == Constants.SerilogTracingActivitySourceName;
    }
    
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
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    protected virtual bool PostSamplingFilter(Activity? activity)
    {
        return true;
    }
    
    /// <inheritdoc />
    void IActivityInstrumentor.InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        if (!ShouldInstrument(activity.Source) || !_replacementSource.CanReplace(activity.Source))
            return;
        
        switch (eventName)
        {
            case Constants.SerilogTracingActivityStartedEventName:
                _replacementSource.StartReplacementActivity(PostSamplingFilter, InstrumentActivity);
                return;
        }
    }
}