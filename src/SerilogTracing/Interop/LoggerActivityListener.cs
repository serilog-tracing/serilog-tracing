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

using System.Diagnostics;
using Serilog;
using Serilog.Events;
using SerilogTracing.Core;
using SerilogTracing.Instrumentation;
using Constants = Serilog.Core.Constants;

namespace SerilogTracing.Interop;

sealed class LoggerActivityListener: IDisposable
{
    readonly ActivityListener? _listener;
    readonly IDisposable? _diagnosticListenerSubscription;

    LoggerActivityListener(ActivityListener? listener, IDisposable? subscription)
    {
        _listener = listener;
        _diagnosticListenerSubscription = subscription;
    }
    
    internal static LoggerActivityListener Configure(
        ActivityListenerConfiguration configuration, Func<ILogger> logger,
        bool ignoreLevelChanges)
    {
        ILogger GetLogger(string name)
        {
            var instance = logger();
            return !string.IsNullOrWhiteSpace(name)
                ? instance.ForContext(Constants.SourceContextPropertyName, name)
                : instance;
        }

        var activityListener = new ActivityListener();
        var subscription = DiagnosticListener.AllListeners.Subscribe(new DiagnosticListenerObserver(configuration.Instrument.GetInstrumentors().ToArray()));

        try
        {
            var levelMap = configuration.InitialLevel.GetOverrideMap();
            var samplingDelegate = configuration.Sample.SamplingDelegate;
            var activityEventRecording = configuration.ActivityEvents.Options;

            samplingDelegate ??= (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                if (options.Parent != default)
                {
                    // The activity is a child of another; if the parent is recorded, the child is recorded. Otherwise,
                    // as long as a local activity is present, there's no need to generate an activity at all.
                    return (options.Parent.TraceFlags & ActivityTraceFlags.Recorded) == ActivityTraceFlags.Recorded ?
                        ActivitySamplingResult.AllDataAndRecorded :
                        options.Parent.IsRemote ?
                            ActivitySamplingResult.PropagationData :
                            ActivitySamplingResult.None;
                }

                return ActivitySamplingResult.AllDataAndRecorded;
            };

            if (ignoreLevelChanges)
            {
                activityListener.ShouldListenTo = source => GetLogger(source.Name)
                    .IsEnabled(GetInitialLevel(levelMap, source.Name));

                activityListener.Sample = samplingDelegate;
            }
            else
            {
                activityListener.ShouldListenTo = _ => true;

                activityListener.Sample = (ref ActivityCreationOptions<ActivityContext> activity) =>
                {
                    if (!GetLogger(activity.Source.Name)
                            .IsEnabled(GetInitialLevel(levelMap, activity.Source.Name)))
                        return ActivitySamplingResult.None;

                    return samplingDelegate(ref activity);
                };
            }

            activityListener.ActivityStopped += activity =>
            {
                if (ActivityInstrumentation.IsDataSuppressed(activity)) return;

                if (ActivityInstrumentation.HasAttachedLoggerActivity(activity))
                    return; // `LoggerActivity` completion writes these to the activity-specific logger.

                var activityLogger = GetLogger(activity.Source.Name);

                var level = GetCompletionLevel(levelMap, activity);

                if (!activityLogger.IsEnabled(level))
                    return;

                if ((activityEventRecording & ActivityEventRecording.AsLogEvents) == ActivityEventRecording.AsLogEvents)
                {
                    var initialLevel = GetInitialLevel(levelMap, activity.Source.Name);
                    var exceptionSkipped = false;
                    
#if FEATURE_ACTIVITY_STRUCTENUMERATORS
                    foreach (var activityEvent in activity.EnumerateEvents())
#else
                    foreach (var activityEvent in activity.Events)
#endif
                    {
                        if (!exceptionSkipped && ActivityInstrumentation.IsException(activityEvent))
                        {
                            exceptionSkipped = true;
                            continue;
                        }
                        
                        activityLogger.Write(ActivityConvert.ActivityEventToLogEvent(activityLogger, activity, activityEvent, initialLevel));
                    }
                }

                activityLogger.Write(ActivityConvert.ActivityToLogEvent(activityLogger, activity, level));
            };

            ActivitySource.AddActivityListener(activityListener);

            return new LoggerActivityListener(activityListener, subscription);
        }
        catch
        {
            activityListener.Dispose();
            subscription.Dispose();
            throw;
        }
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

    public void Dispose()
    {
        _listener?.Dispose();
        _diagnosticListenerSubscription?.Dispose();
    }
}