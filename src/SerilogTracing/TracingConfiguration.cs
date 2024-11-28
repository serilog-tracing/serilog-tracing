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
using Serilog;
using Serilog.Events;
using SerilogTracing.Configuration;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace SerilogTracing;

/// <summary>
/// Configure integration between SerilogTracing and the .NET tracing infrastructure.
/// </summary>
[Obsolete("This type has been renamed to ActivityListenerConfiguration; use that name instead.")]
// ReSharper disable once UnusedType.Global
public class TracingConfiguration
{
    readonly ActivityListenerConfiguration _inner = new();

    /// <summary>
    /// Configures instrumentation of <see cref="Activity">activities</see>.
    /// </summary>
    public ActivityListenerInstrumentationConfiguration Instrument => _inner.Instrument;

    /// <summary>
    /// Configures sampling.
    /// </summary>
    public ActivityListenerSamplingConfiguration Sample => _inner.Sample;

    /// <summary>
    /// Configures the initial level assigned to externally-created activities. Setting the level of an external
    /// activity source to a lower value, such as <see cref="LogEventLevel.Debug"/>, causes activities from that
    /// source to be suppressed when the level is not enabled.
    /// </summary>
    public ActivityListenerInitialLevelConfiguration InitialLevel => _inner.InitialLevel;

    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// </summary>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable EnableTracing(ILogger? logger = null)
    {
        return logger != null ? TraceTo(logger) : TraceToSharedLogger();
    }
    
    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// </summary>
    /// <param name="logger">
    /// The logger instance to emit traces through. Avoid using the shared <see cref="Log.Logger" /> as
    /// the value here. To emit traces through the shared static logger, call <see cref="TraceToSharedLogger" /> instead.
    /// </param>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceTo(ILogger logger) => _inner.TraceTo(logger);

    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// This method is not equivalent to <code>TraceTo(Log.Logger)</code>. The former will emit traces through whatever the
    /// value of <see cref="Log.Logger" /> happened to be at the time <see cref="TraceTo" /> was called. This method
    /// will always emit traces through the current value of <see cref="Log.Logger" />.
    /// </summary>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceToSharedLogger() => _inner.TraceToSharedLogger();
}
