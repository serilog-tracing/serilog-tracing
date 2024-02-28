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
using SerilogTracing.Configuration;
using SerilogTracing.Interop;

namespace SerilogTracing;

/// <summary>
/// Configure integration between SerilogTracing and the .NET tracing infrastructure.
/// </summary>
/// <remarks>
/// <p>
///     There are two main integration points between SerilogTracing and the rest of
///     .NET.
/// </p>
/// <p>
///     The first is the <c>"Serilog"</c> <see cref="ActivitySource"/>, which the
///     <see cref="LoggerTracingExtensions.StartActivity(ILogger, string)"/> method uses to
///     publish activities created using SerilogTracing to the rest of .NET."/>
/// </p>
/// <p>
///     The second integration point, which this type, <see cref="ActivityListenerConfiguration"/>
///     configures, is an <see cref="ActivityListener"/> through which SerilogTracing
///     receives activities from other .NET components.
/// </p>
/// <p>
///     Configuration options specified using this type always apply to external activities from
///     other .NET components. In cases where these options also apply to SerilogTracing's own
///     activities, the configuration sub-object (for example <see cref="Sample"/>) will carry
///     documentation describing this.
/// </p>
/// </remarks>
public class ActivityListenerConfiguration
{
    /// <summary>
    /// Construct a new <see cref="ActivityListenerConfiguration" />.
    /// </summary>
    public ActivityListenerConfiguration()
    {
        Instrument = new ActivityListenerInstrumentationConfiguration(this);
        Sample = new ActivityListenerSamplingConfiguration(this);
        InitialLevel = new ActivityListenerInitialLevelConfiguration(this);
    }

    /// <summary>
    /// Configures instrumentation applied to externally-created activities.
    /// </summary>
    public ActivityListenerInstrumentationConfiguration Instrument { get; }

    /// <summary>
    /// Configures sampling. These options apply to both external activities from other .NET components,
    /// and to the `"Serilog"` activity source that produces SerilogTracing's own activities.
    /// </summary>
    public ActivityListenerSamplingConfiguration Sample { get; }

    /// <summary>
    /// Configures the initial level assigned to externally-created activities.
    /// </summary>
    public ActivityListenerInitialLevelConfiguration InitialLevel { get; }

    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// </summary>
    /// <param name="logger">
    /// The logger instance to emit traces through. Avoid using the shared <see cref="Log.Logger" /> as
    /// the value here. To emit traces through the shared static logger, call <see cref="TraceToSharedLogger" /> instead.
    /// </param>
    /// <param name="ignoreLevelChanges">If <c langword="true"/>, the activity listener will assume all initial and
    /// minimum levels are fixed at the time of activity listener creation. This may slightly improve tracing performance.</param>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceTo(ILogger logger, bool ignoreLevelChanges = false)
    {
        return LoggerActivityListener.Configure(this, () => logger, ignoreLevelChanges);
    }

    /// <summary>
    /// Completes configuration and returns a handle that can be used to shut tracing down when no longer required.
    /// This method is not equivalent to <code>TraceTo(Log.Logger)</code>. The former will emit traces through whatever the
    /// value of <see cref="Log.Logger" /> happened to be at the time <see cref="TraceTo" /> was called. This method
    /// will always emit traces through the current value of <see cref="Log.Logger" />.
    /// </summary>
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceToSharedLogger()
    {
        return LoggerActivityListener.Configure(this, () => Log.Logger, false);
    }
}