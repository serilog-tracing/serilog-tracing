// Based on code from Serilog.Sinks.Seq with modifications by
// SerilogTracing contributors.

// Serilog.Sinks.Seq Copyright © Serilog Contributors
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
using Serilog.Core;
using Serilog.Events;

namespace SerilogTracing;

/// <summary>
/// Extends Serilog configuration to write og events and traces to Seq.
/// </summary>
public static class SeqTracingLoggerSinkConfigurationExtensions
{
    /// <summary>
    /// Write log events and traces to a <a href="https://datalust.co/seq">Seq</a> server.
    /// </summary>
    /// <param name="loggerSinkConfiguration">The logger configuration.</param>
    /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
    /// <param name="restrictedToMinimumLevel">The minimum log event level required
    /// in order to write an event to the sink.</param>
    /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
    /// <param name="period">The time to wait between checking for event batches.</param>
    /// <param name="bufferBaseFilename">Path for a set of files that will be used to buffer events until they
    /// can be successfully transmitted across the network. Individual files will be created using the
    /// pattern <paramref name="bufferBaseFilename" />*.json, which should not clash with any other filenames
    /// in the same directory.</param>
    /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
    /// <param name="bufferSizeLimitBytes">The maximum amount of data, in bytes, to which the buffer
    /// log file for a specific date will be allowed to grow. By default no limit will be applied.</param>
    /// <param name="eventBodyLimitBytes">The maximum size, in bytes, that the JSON representation of
    /// an event may take before it is dropped rather than being sent to the Seq server. Specify null for no limit.
    /// The default is 265 KB.</param>
    /// <param name="controlLevelSwitch">If provided, the switch will be updated based on the Seq server's level setting
    /// for the corresponding API key. Passing the same key to MinimumLevel.ControlledBy() will make the whole pipeline
    /// dynamically controlled. Do not specify <paramref name="restrictedToMinimumLevel" /> with this setting.</param>
    /// <param name="messageHandler">Used to construct the HttpClient that will send the log messages to Seq.</param>
    /// <param name="retainedInvalidPayloadsLimitBytes">A soft limit for the number of bytes to use for storing failed requests.
    /// The limit is soft in that it can be exceeded by any single error payload, but in that case only that single error
    /// payload will be retained.</param>
    /// <param name="queueSizeLimit">The maximum number of events that will be held in-memory while waiting to ship them to
    /// Seq. Beyond this limit, events will be dropped. The default is 100,000. Has no effect on
    /// durable log shipping.</param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    /// <exception cref="T:System.ArgumentNullException">A required parameter is null.</exception>
    /// <remarks>
    /// The functionality of this method is now provided by Serilog.Sinks.Seq version 7+.
    /// </remarks>
    // Soon: [Obsolete("Use Serilog.Sinks.Seq version 7.0.0 or later, and WriteTo.Seq() instead.")]
    public static LoggerConfiguration SeqTracing(
      this LoggerSinkConfiguration loggerSinkConfiguration,
      string serverUrl,
      LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
      int batchPostingLimit = 1000,
      TimeSpan? period = null,
      string? apiKey = null,
      string? bufferBaseFilename = null,
      long? bufferSizeLimitBytes = null,
      long? eventBodyLimitBytes = 262144,
      LoggingLevelSwitch? controlLevelSwitch = null,
      HttpMessageHandler? messageHandler = null,
      long? retainedInvalidPayloadsLimitBytes = null,
      int queueSizeLimit = 100000)
    {
        if (loggerSinkConfiguration == null)
            throw new ArgumentNullException(nameof(loggerSinkConfiguration));

        return loggerSinkConfiguration.Seq(
            serverUrl,
            restrictedToMinimumLevel,
            batchPostingLimit,
            period,
            apiKey,
            bufferBaseFilename,
            bufferSizeLimitBytes,
            eventBodyLimitBytes,
            controlLevelSwitch,
            messageHandler,
            retainedInvalidPayloadsLimitBytes,
            queueSizeLimit);
    }
}
