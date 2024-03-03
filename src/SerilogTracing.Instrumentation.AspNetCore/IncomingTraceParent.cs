namespace SerilogTracing;

/// <summary>
/// How to treat incoming trace context on the <a href="https://www.w3.org/TR/trace-context/">traceparent</a> header.
/// </summary>
public enum IncomingTraceParent
{
    /// <summary>
    /// Ignore any trace context, generating a new root <see cref="System.Diagnostics.Activity"/> for each incoming request.
    /// </summary>
    /// <remarks>
    /// This scheme prevents any distributed tracing from taking place by ignoring any context that would be propagated.
    /// It's suitable for publicly accessible services where clients may craft malicious traceparent headers.
    /// </remarks>
    Ignore,
    
    /// <summary>
    /// Accept the trace id and parent span id, ignoring trace flags and baggage. This is the default scheme.
    /// </summary>
    /// <remarks>
    /// A new sibling <see cref="System.Diagnostics.Activity"/> with the trace id and parent span ids will be generated.
    ///
    /// This scheme offers a balance between safety against malicious clients and participation in distributed tracing.
    /// Traces will form a proper hierarchy unless that trace was marked as not recorded by a parent. In that case,
    /// any spans produced by a system using this scheme will have missing parents.
    ///
    /// This scheme is a reasonable default, but production systems should use either <see cref="IncomingTraceParent.Ignore"/>
    /// or <see cref="IncomingTraceParent.Trust"/> depending on their level of public accessibility.
    /// </remarks>
    Accept,
    
    /// <summary>
    /// Accept the trace id, parent span id, trace flags and baggage.
    /// </summary>
    /// <remarks>
    /// A new sibling <see cref="System.Diagnostics.Activity"/> with the trace context of the original will be
    /// generated for requests with no traceparent header.
    ///
    /// This scheme is suitable for services that aren't publicly accessible.
    /// </remarks>
    Trust
}