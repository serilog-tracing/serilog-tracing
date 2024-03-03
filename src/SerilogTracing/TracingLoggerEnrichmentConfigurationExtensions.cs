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

using Serilog;
using Serilog.Configuration;
using SerilogTracing.Enrichers;

namespace SerilogTracing;

/// <summary>
/// Enrichment for spans.
/// </summary>
public static class TracingLoggerEnrichmentConfigurationExtensions
{
    /// <summary>
    /// Enrich log events with the span duration as milliseconds.
    /// </summary>
    /// <param name="enrichment">The configuration object.</param>
    /// <param name="propertyName">The name of the property to add.</param>
    public static LoggerConfiguration WithSpanTimingMilliseconds(this LoggerEnrichmentConfiguration enrichment, string? propertyName = null)
    {
        return enrichment.With(new SpanTimingMillisecondsEnricher(propertyName ?? "Elapsed"));
    }

    /// <summary>
    /// Enrich log events with the span duration as a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="enrichment">The configuration object.</param>
    /// <param name="propertyName">The name of the property to add.</param>
    public static LoggerConfiguration WithSpanTiming(this LoggerEnrichmentConfiguration enrichment, string? propertyName = null)
    {
        return enrichment.With(new SpanTimingEnricher(propertyName ?? "Elapsed"));
    }
}
