using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// An activity enricher that populates the current activity with context from incoming HTTP requests.
/// </summary>
public sealed class HttpRequestInActivityEnricher: IActivityEnricher
{
    /// <summary>
    /// Create an instance of the enricher.
    /// </summary>
    public HttpRequestInActivityEnricher()
    {
        
    }
    
    /// <inheritdoc cref="IActivityEnricher.ShouldListenTo"/>
    public bool ShouldListenTo(string listenerName)
    {
        return listenerName == "Microsoft.AspNetCore";
    }

    /// <inheritdoc cref="IActivityEnricher.ShouldListenTo"/>
    public void EnrichActivity(Activity activity, string eventName, object eventArgs)
    {
        if (eventArgs is HttpContext ctxt)
        {
            throw new NotImplementedException();
        }
    }
}