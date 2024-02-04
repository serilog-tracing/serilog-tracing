﻿using Microsoft.Data.SqlClient;
using SerilogTracing.Configuration;
using SerilogTracing.Instrumentation.SqlClient;

namespace SerilogTracing;

/// <summary>
/// Extends <see cref="TracingInstrumentationConfiguration"/> with methods to support ASP.NET
/// Core instrumentation.
/// </summary>
public static class TracingInstrumentationConfigurationSqlClientExtensions
{
    /// <summary>
    /// Add instrumentation for <see cref="SqlCommand"/> commands.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static TracingConfiguration SqlClientCommands(this TracingInstrumentationConfiguration configuration)
    {
        return configuration.With(new SqlCommandActivityInstrumentor(new ()));
    }
    
    /// <summary>
    /// Add instrumentation for <see cref="SqlCommand"/> commands.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configure">A callback to configure the instrumentation.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static TracingConfiguration SqlClientCommands(
        this TracingInstrumentationConfiguration configuration, Action<SqlCommandActivityInstrumentationOptions> configure)
    {
        var options = new SqlCommandActivityInstrumentationOptions();
        configure.Invoke(options);
        
        return configuration.With(new SqlCommandActivityInstrumentor(options));
    }
}