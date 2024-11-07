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
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    protected abstract bool ShouldReplace(ActivitySource source);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="replacementActivity"></param>
    protected abstract void InstrumentReplacementActivity(Activity replacementActivity);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="replacementActivity"></param>
    /// <returns></returns>
    protected virtual bool PostSamplingReplacementFilter(Activity? replacementActivity)
    {
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activityToReplace"></param>
    /// <returns></returns>
    protected virtual ReplacementActivityOptions ReplacementOptions(Activity activityToReplace)
    {
        return new ReplacementActivityOptions();
    }
    
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
                if (!ShouldReplace(activity.Source) || !_replacementSource.CanReplace(activity.Source))
                    return;
                
                _replacementSource.StartReplacementActivity(
                    PostSamplingReplacementFilter,
                    InstrumentReplacementActivity,
                    ReplacementOptions(activity)
                );
                
                return;
        }
    }
}