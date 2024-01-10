using System.Diagnostics;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// 
/// </summary>
public sealed class HttpRequestInActivityEnricher: IActivityEnricher
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="listenerName"></param>
    /// <returns></returns>
    public bool SubscribeTo(string listenerName)
    {
        return listenerName == "Microsoft.AspNetCore";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="eventName"></param>
    /// <param name="eventArgs"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void EnrichActivity(Activity activity, string eventName, object eventArgs)
    {
        throw new NotImplementedException();
    }
}