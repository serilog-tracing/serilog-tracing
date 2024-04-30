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
            new LogEventProperty("RequestPath", new ScalarValue(request.Path)),
        };

    static IEnumerable<LogEventProperty> DefaultGetResponseProperties(HttpResponse response) =>
        new[]
        {
            new LogEventProperty("StatusCode", new ScalarValue(response.StatusCode)),
        };

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
}