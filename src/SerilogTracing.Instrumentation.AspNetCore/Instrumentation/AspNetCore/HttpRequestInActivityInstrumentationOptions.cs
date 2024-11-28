// Copyright © SerilogTracing Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// Configuration for ASP.NET Core HTTP request instrumentation.
/// </summary>
public sealed class HttpRequestInActivityInstrumentationOptions
{
    const string DefaultRequestCompletionMessageTemplate =
        "HTTP {RequestMethod} {RequestPath}";

    static IEnumerable<LogEventProperty> DefaultGetRequestProperties(HttpRequest request) =>
        new[]
        {
            new LogEventProperty("RequestMethod", new ScalarValue(request.Method)),
            // `request.Path` is a `PathString` struct; we convert to `string` so that the resulting property value
            // is easier to work with (i.e. in filter expressions).
            new LogEventProperty("RequestPath", new ScalarValue(request.Path.ToString())),
        };

    static readonly LogEventProperty RequestAbortedTrue = new("RequestAborted", new ScalarValue(true));
    static IEnumerable<LogEventProperty> DefaultGetResponseProperties(HttpResponse response)
    {
        var statusCode = new LogEventProperty("StatusCode", new ScalarValue(response.StatusCode));
        return response.HttpContext.RequestAborted.IsCancellationRequested ? new []
        {
            statusCode,
            RequestAbortedTrue,
        } : [statusCode];
    }

    static bool DefaultIsErrorResponse(HttpResponse response) =>
        response is { StatusCode: >= 500, HttpContext.RequestAborted.IsCancellationRequested: false };

    /// <summary>
    /// What distributed context to respect from incoming traceparent headers.
    ///
    /// See the <see cref="IncomingTraceParent"/> type for details on available options.
    /// </summary>
    public IncomingTraceParent IncomingTraceParent { get; set; } = IncomingTraceParent.Accept;

    /// <summary>
    /// The message template to associate with request activities.
    /// </summary>
    public string MessageTemplate { get; set; } = DefaultRequestCompletionMessageTemplate;

    /// <summary>
    /// A function to populate properties on the activity from an incoming request.
    ///
    /// This closure will be invoked at the start of the request pipeline.
    /// </summary>
    public Func<HttpRequest, IEnumerable<LogEventProperty>> GetRequestProperties { get; set; } = DefaultGetRequestProperties;

    /// <summary>
    /// A function to populate properties on the activity from an outgoing response.
    ///
    /// This closure will be invoked at the end of the request pipeline.
    /// </summary>
    public Func<HttpResponse, IEnumerable<LogEventProperty>> GetResponseProperties { get; set; } = DefaultGetResponseProperties;

    /// <summary>
    /// A callback to determine whether the given HTTP response indicates a failed request. The default returns
    /// <c langword="true"/> if the status code is 500 or greater, and the request was not aborted.
    /// </summary>
    public Func<HttpResponse, bool> IsErrorResponse { get; set; } = DefaultIsErrorResponse;
    
    /// <summary>
    /// An additional filter that will be applied after sampling. If the filter returns <c langword="false"/> then
    /// the sampling decision and parent sampling flags (if applicable) will be ignored, and the activity will
    /// be suppressed. Use this to disable tracing entirely for specific routes or callers.
    /// </summary>
    public Func<HttpContext, bool>? PostSamplingFilter { get; set; }
}