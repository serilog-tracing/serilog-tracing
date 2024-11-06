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

using System.Diagnostics;
using SerilogTracing.Instrumentation;
using SerilogTracing.Instrumentation.HttpClient;

namespace SerilogTracing.Configuration;

/// <summary>
/// Controls instrumentation configuration.
/// </summary>
public sealed class ActivityListenerInstrumentationConfiguration
{
    readonly ActivityListenerConfiguration _activityListenerConfiguration;
    readonly List<IActivityInstrumentor> _instrumentors = [];
    bool _withDefaultInstrumentors = true;

    static IEnumerable<IActivityInstrumentor> GetDefaultInstrumentors() => [new HttpRequestOutActivityInstrumentor(new HttpRequestOutActivityInstrumentationOptions())];

    internal IEnumerable<IActivityInstrumentor> GetInstrumentors() =>
        _withDefaultInstrumentors ?
            GetDefaultInstrumentors().Concat(_instrumentors) : _instrumentors;

    internal ActivityListenerInstrumentationConfiguration(ActivityListenerConfiguration activityListenerConfiguration)
    {
        _activityListenerConfiguration = activityListenerConfiguration;
    }

    /// <summary>
    /// Whether to use default built-in activity instrumentors.
    /// </summary>
    /// <param name="withDefaults">If true, default activity instrumentors will be used.</param>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration WithDefaultInstrumentation(bool withDefaults)
    {
        _withDefaultInstrumentors = withDefaults;
        return _activityListenerConfiguration;
    }

    /// <summary>
    /// Specifies one or more instrumentors that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <param name="instrumentors">Instrumentors to apply to all events passing through
    /// the logger.</param>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instrumentors"/> is <code>null</code></exception>
    /// <exception cref="ArgumentException">When any element of <paramref name="instrumentors"/> is <code>null</code></exception>
    public ActivityListenerConfiguration With(params IActivityInstrumentor[] instrumentors)
    {
        if (instrumentors == null)
        {
            throw new ArgumentNullException(nameof(instrumentors));
        }

        foreach (var instrumentor in instrumentors)
        {
            if (instrumentor == null)
            {
                throw new ArgumentNullException(nameof(instrumentors));
            }

            _instrumentors.Add(instrumentor);
        }

        return _activityListenerConfiguration;
    }

    /// <summary>
    /// Specifies an instrumentor that may add properties dynamically to
    /// log events.
    /// </summary>
    /// <typeparam name="TInstrumentor">Instrumentor type to apply to all events passing through
    /// the logger.</typeparam>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration With<TInstrumentor>()
        where TInstrumentor : IActivityInstrumentor, new()
    {
        return With(new TInstrumentor());
    }

    /// <summary>
    /// 
    /// </summary>
    public ActivityListenerConfiguration ActivitySource(string activitySourceName, Action<Activity> instrument)
    {
        return With(new ActivitySourceInstrumentor(source => source.Name == activitySourceName, instrument, null));
    }
}
