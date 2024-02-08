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
using Serilog.Debugging;
using Serilog.Events;
using SerilogTracing.Configuration;
using SerilogTracing.Core;
using SerilogTracing.Instrumentation;
using SerilogTracing.Interop;
using Constants = Serilog.Core.Constants;

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
    /// <returns>A handle that must be kept alive while tracing is required, and disposed afterwards.</returns>
    public IDisposable TraceTo(ILogger logger)
    {
        return EnableTracing(() => logger);
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
        return EnableTracing(() => Log.Logger);
    }

    IDisposable EnableTracing(Func<ILogger> logger)
    {
        ILogger GetLogger(string name)
        {
            var instance = logger();
            return !string.IsNullOrWhiteSpace(name)
                ? instance.ForContext(Constants.SourceContextPropertyName, name)
                : instance;
        }

        var activityListener = new ActivityListener();
        var disposeProxy = new DisposeProxy(activityListener,
            DiagnosticListener.AllListeners.Subscribe(
                new DiagnosticListenerObserver(Instrument.GetInstrumentors().ToArray())));

        var levelMap = InitialLevel.GetOverrideMap();

        // We may want an opt-in to performing level checks eagerly here.
        // It would be a performance win, but would also prevent dynamic log level changes from being effective.
        activityListener.ShouldListenTo = _ => true;

        var sample = Sample.ActivityContext;
        activityListener.Sample = (ref ActivityCreationOptions<ActivityContext> activity) =>
        {
            if (!GetLogger(activity.Source.Name)
                    .IsEnabled(GetInitialLevel(levelMap, activity.Source.Name)))
                return ActivitySamplingResult.None;

            return sample?.Invoke(ref activity) ?? ActivitySamplingResult.AllData;
        };

        activityListener.ActivityStopped += activity =>
        {
            if (!activity.Recorded) return;

            if (ActivityInstrumentation.HasAttachedLoggerActivity(activity))
                return; // `LoggerActivity` completion writes these to the activity-specific logger.

            var activityLogger = GetLogger(activity.Source.Name);

            var level = GetCompletionLevel(levelMap, activity);

            if (!activityLogger.IsEnabled(level))
                return;

            activityLogger.Write(ActivityConvert.ActivityToLogEvent(activityLogger, activity, level));
        };

        ActivitySource.AddActivityListener(activityListener);

        return disposeProxy;
    }

    static LogEventLevel GetInitialLevel(LevelOverrideMap levelMap, string activitySourceName)
    {
        levelMap.GetEffectiveLevel(activitySourceName, out var initialLevel, out var overrideLevel);

        return overrideLevel?.MinimumLevel ?? initialLevel;
    }

    static LogEventLevel GetCompletionLevel(LevelOverrideMap levelMap, Activity activity)
    {
        var level = GetInitialLevel(levelMap, activity.Source.Name);

        if (activity.Status == ActivityStatusCode.Error && level < LogEventLevel.Error)
        {
            return LogEventLevel.Error;
        }

        return level;
    }

    sealed class DisposeProxy(params IDisposable[] disposables) : IDisposable
    {
        public void Dispose()
        {
            foreach (var disposable in disposables)
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("TracingLogger: exception in dispose" + Environment.NewLine + ex);
                }
        }
    }
}