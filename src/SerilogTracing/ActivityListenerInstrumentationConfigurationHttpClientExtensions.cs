﻿// Copyright © SerilogTracing Contributors
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

using SerilogTracing.Configuration;
using SerilogTracing.Instrumentation.HttpClient;

namespace SerilogTracing;

/// <summary>
/// Support ASP.NET Core instrumentation.
/// </summary>
public static class ActivityListenerInstrumentationConfigurationHttpClientExtensions
{
    /// <summary>
    /// Add instrumentation for <see cref="HttpClient"/> requests.
    /// </summary>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static ActivityListenerConfiguration HttpClientRequests(this ActivityListenerInstrumentationConfiguration configuration)
    {
        return configuration.HttpClientRequests(_ => { });
    }
    
    /// <summary>
    /// Add instrumentation for <see cref="HttpClient"/> requests.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configure">A callback to configure the instrumentation.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static ActivityListenerConfiguration HttpClientRequests(
        this ActivityListenerInstrumentationConfiguration configuration, Action<HttpRequestOutActivityInstrumentationOptions> configure)
    {
        var httpOptions = new HttpRequestOutActivityInstrumentationOptions();
        configure.Invoke(httpOptions);

        return configuration.With(new HttpRequestOutActivityInstrumentor(httpOptions));
    }
}
