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

using Serilog.Core;
using Serilog.Events;
using SerilogTracing.Core;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace SerilogTracing.Configuration;

/// <summary>
/// Controls initial level configuration.
/// </summary>
public class ActivityListenerInitialLevelConfiguration
{
    readonly ActivityListenerConfiguration _activityListenerConfiguration;
    readonly Dictionary<string, LoggingLevelSwitch> _overrides = new();
    LogEventLevel _initialLevel = LogEventLevel.Information;
    bool _ignoreLevelChanges;

    internal LevelOverrideMap GetOverrideMap() => new(_overrides, _initialLevel, null);
    internal bool IgnoreLevelChanges => _ignoreLevelChanges;

    internal ActivityListenerInitialLevelConfiguration(ActivityListenerConfiguration activityListenerConfiguration)
    {
        _activityListenerConfiguration = activityListenerConfiguration;
    }

    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities.
    /// </summary>
    /// <param name="level">The initial level to set.</param>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Is(LogEventLevel level)
    {
        _initialLevel = level;
        return _activityListenerConfiguration;
    }
    
    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities to <see cref="LogEventLevel.Verbose"/>.
    /// </summary>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Verbose()
    {
        return Is(LogEventLevel.Verbose);
    }
    
    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities to <see cref="LogEventLevel.Debug"/>.
    /// </summary>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Debug()
    {
        return Is(LogEventLevel.Debug);
    }
    
    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities to <see cref="LogEventLevel.Information"/>.
    /// </summary>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Information()
    {
        return Is(LogEventLevel.Information);
    }
    
    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities to <see cref="LogEventLevel.Warning"/>.
    /// </summary>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Warning()
    {
        return Is(LogEventLevel.Warning);
    }
    
    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities to <see cref="LogEventLevel.Error"/>.
    /// </summary>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Error()
    {
        return Is(LogEventLevel.Error);
    }
    
    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities to <see cref="LogEventLevel.Fatal"/>.
    /// </summary>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Fatal()
    {
        return Is(LogEventLevel.Fatal);
    }

    /// <summary>
    /// Override the initial level for activities from a specific <see cref="System.Diagnostics.ActivitySource"/>.
    /// </summary>
    /// <param name="activitySourceName">
    /// The (partial) name of the <see cref="System.Diagnostics.ActivitySource"/> to override for. Prefixes are assumed
    /// to be namespace sub-paths, such as <code>Microsoft</code> in <code>Microsoft.AspNetCore</code>.
    /// </param>
    /// <param name="levelSwitch">The initial level to set.</param>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Override(string activitySourceName, LoggingLevelSwitch levelSwitch)
    {
        _overrides[activitySourceName] = levelSwitch;
        return _activityListenerConfiguration;
    }

    /// <summary>
    /// Override the initial level for activities from a specific <see cref="System.Diagnostics.ActivitySource"/>.
    /// </summary>
    /// <param name="activitySourceName">
    /// The (partial) name of the <see cref="System.Diagnostics.ActivitySource"/> to override for. Prefixes are assumed
    /// to be namespace sub-paths, such as <code>Microsoft</code> in <code>Microsoft.AspNetCore</code>.
    /// </param>
    /// <param name="level">The initial level to set.</param>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration Override(string activitySourceName, LogEventLevel level)
    {
        return Override(activitySourceName, new LoggingLevelSwitch(level));
    }

    /// <summary>
    /// The first time an external activity source is encountered, check whether its initial level is enabled, and
    /// cache this decision for the remainder of the listener's lifetime. This can be a significant performance and
    /// memory usage optimization for apps that don't modify the target logger's minimum level at runtime.
    /// </summary>
    /// <remarks>If your application is using dynamic level control, either with <see cref="LoggingLevelSwitch"/>,
    /// reloadable/bootstrap loggers, or by responding to configuration file changes at runtime, don't apply this
    /// option.</remarks>
    /// <returns>The activity listener configuration, to enable method chaining.</returns>
    public ActivityListenerConfiguration IgnoreChanges()
    {
        _ignoreLevelChanges = true;
        return _activityListenerConfiguration;
    }
}
