using Serilog.Core;
using Serilog.Events;
using SerilogTracing.Core;

namespace SerilogTracing.Configuration;

/// <summary>
/// Controls initial level configuration.
/// </summary>
public class ActivityListenerInitialLevelConfiguration
{
    readonly ActivityListenerConfiguration _activityListenerConfiguration;
    readonly Dictionary<string, LoggingLevelSwitch> _overrides = new();
    LogEventLevel _initialLevel = LogEventLevel.Information;

    internal LevelOverrideMap GetOverrideMap() => new(_overrides, _initialLevel, null);
    
    internal ActivityListenerInitialLevelConfiguration(ActivityListenerConfiguration activityListenerConfiguration)
    {
        _activityListenerConfiguration = activityListenerConfiguration;
    }

    /// <summary>
    /// Sets the initial level that will be assigned to externally created activities.
    /// </summary>
    /// <param name="level">The initial level to set.</param>
    public ActivityListenerConfiguration Is(LogEventLevel level)
    {
        _initialLevel = level;
        return _activityListenerConfiguration;
    }

    /// <summary>
    /// Override the initial level for activities from a specific <see cref="System.Diagnostics.ActivitySource"/>.
    /// </summary>
    /// <param name="activitySourceName">
    /// The (partial) name of the <see cref="System.Diagnostics.ActivitySource"/> to override for. Prefixes are assumed
    /// to be namespace sub-paths, such as <code>Microsoft</code> in <code>Microsoft.AspNetCore</code>.
    /// </param>
    /// <param name="levelSwitch">The initial level to set.</param>
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
    public ActivityListenerConfiguration Override(string activitySourceName, LogEventLevel level)
    {
        return Override(activitySourceName, new LoggingLevelSwitch(level));
    }
}