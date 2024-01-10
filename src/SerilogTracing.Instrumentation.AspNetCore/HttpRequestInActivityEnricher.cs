using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Interop;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// An activity enricher that populates the current activity with context from incoming HTTP requests.
/// </summary>
public sealed class HttpRequestInActivityEnricher: IActivityEnricher
{
    /// <summary>
    /// Create an instance of the enricher.
    /// </summary>
    public HttpRequestInActivityEnricher() {}

    /// <inheritdoc cref="IActivityEnricher.ShouldListenTo"/>
    public bool ShouldListenTo(string listenerName)
    {
        return listenerName == "Microsoft.AspNetCore";
    }

    /// <inheritdoc cref="IActivityEnricher.ShouldListenTo"/>
    public void EnrichActivity(Activity activity, string eventName, object eventArgs)
    {
        if (eventArgs is not HttpContext ctxt) return;

        switch (eventName)
        {
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                activity.SetMessageTemplateOverride(MessageTemplateOverride);
                activity.DisplayName = MessageTemplateOverride.Text;

                activity.SetTag("RequestMethod", ctxt.Request.Method);
                activity.SetTag("RequestUri", ctxt.Request.GetDisplayUrl());
                activity.SetTag("StatusCode", null);
                    
                break;
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                activity.SetTag("StatusCode", ctxt.Response.StatusCode);
                activity.SetTag("ContentLength", ctxt.Response.ContentLength);

                break;
        }
    }

    static readonly MessageTemplate MessageTemplateOverride =
        new MessageTemplateParser().Parse("HTTP {RequestMethod} {RequestUri} In");
}