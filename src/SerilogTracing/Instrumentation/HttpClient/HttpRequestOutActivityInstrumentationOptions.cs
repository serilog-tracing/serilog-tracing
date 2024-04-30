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

using Serilog.Events;

namespace SerilogTracing.Instrumentation.HttpClient;

/// <summary>
/// Configuration for <see cref="HttpClient"/> HTTP request instrumentation.
/// </summary>
public class HttpRequestOutActivityInstrumentationOptions
{
    static bool DefaultIsErrorResponse(HttpResponseMessage response) => (int)response.StatusCode >= 400;
    
    const string DefaultRequestCompletionMessageTemplate = "HTTP {RequestMethod} {RequestUri}";

    static IEnumerable<LogEventProperty> DefaultGetRequestProperties(HttpRequestMessage request)
    {
        // User, query, and fragment are trimmed by default to reduce information leakage.
        
        var uriBuilder = request.RequestUri == null ? null : new UriBuilder(request.RequestUri)
        {
            Query = null!,
            Fragment = null!,
            UserName = null!,
            Password = null!
        };

        return new[]
        {
            new LogEventProperty("RequestMethod", new ScalarValue(request.Method)),
            new LogEventProperty("RequestUri", new ScalarValue(uriBuilder?.Uri))
        };
    }

    static IEnumerable<LogEventProperty> DefaultGetResponseProperties(HttpResponseMessage? response) =>
        new[]
        {
            new LogEventProperty("StatusCode", new ScalarValue(response?.StatusCode)),
        };

    /// <summary>
    /// The message template to associate with request activities.
    /// </summary>
    public string MessageTemplate { get; set; } = DefaultRequestCompletionMessageTemplate;

    /// <summary>
    /// A function to populate properties on the activity from an outgoing request.
    /// </summary>
    public Func<HttpRequestMessage, IEnumerable<LogEventProperty>> GetRequestProperties { get; set; } = DefaultGetRequestProperties;

    /// <summary>
    /// A function to populate properties on the activity from an incoming response.
    /// </summary>
    public Func<HttpResponseMessage?, IEnumerable<LogEventProperty>> GetResponseProperties { get; set; } = DefaultGetResponseProperties;
    
    /// <summary>
    /// A callback that determines whether the given <see cref="HttpResponseMessage"/> is considered to be
    /// an error. The default implementation returns <c langword="true" /> when the response status code
    /// is greater than or equal to 400.
    /// </summary>
    /// <remarks>Requests that fail due to unhandled exceptions are always considered to have errored, and this callback
    /// is ignored in those cases.</remarks>
    public Func<HttpResponseMessage, bool> IsErrorResponse { get; set; } = DefaultIsErrorResponse;
}
