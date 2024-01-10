// Copyright 2022 Serilog Contributors
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

using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace SerilogTracing.Sinks.OpenTelemetry.Exporters;

/// <summary>
/// Implements an IExporter that sends OpenTelemetry Log requests
/// over gRPC.
/// </summary>
sealed class GrpcExporter : IExporter, IDisposable
{
    readonly string _logsEndpoint;
    readonly string _tracesEndpoint;

    readonly GrpcChannel _logsChannel;
    readonly GrpcChannel _tracesChannel;

    readonly LogsService.LogsServiceClient _logsClient;
    readonly TraceService.TraceServiceClient _tracesClient;
    
    readonly Metadata _headers;

    /// <summary>
    /// Creates a new instance of a GrpcExporter that writes an
    /// ExportLogsServiceRequest to a gRPC endpoint.
    /// </summary>
    /// <param name="logsEndpoint">
    /// The full OTLP endpoint to which logs are sent.
    /// </param>
    /// <param name="tracesEndpoint">
    /// The full OTLP endpoint to which traces are sent.
    /// </param>
    /// <param name="headers">
    /// A dictionary containing the request headers.
    /// </param>
    /// <param name="httpMessageHandler">
    /// Custom HTTP message handler.
    /// </param>
    public GrpcExporter(string logsEndpoint, string tracesEndpoint, IReadOnlyDictionary<string, string> headers,
        HttpMessageHandler? httpMessageHandler = null)
    {
        _logsEndpoint = string.IsNullOrWhiteSpace(logsEndpoint) ? tracesEndpoint : logsEndpoint;
        _tracesEndpoint = string.IsNullOrWhiteSpace(tracesEndpoint) ? logsEndpoint : tracesEndpoint;
        
        var grpcChannelOptions = new GrpcChannelOptions();
        if (httpMessageHandler != null)
        {
            grpcChannelOptions.HttpClient = new HttpClient(httpMessageHandler);
            grpcChannelOptions.DisposeHttpClient = true;
        }
        
        _logsChannel = GrpcChannel.ForAddress(_logsEndpoint, grpcChannelOptions);
        _logsClient = new LogsService.LogsServiceClient(_logsChannel);
        _tracesChannel = GrpcChannel.ForAddress(_tracesEndpoint, grpcChannelOptions);
        _tracesClient = new TraceService.TraceServiceClient(_logsChannel);

        _headers = new Metadata();
        foreach (var header in headers)
        {
            _headers.Add(header.Key, header.Value);
        }
    }

    public void Dispose()
    {
        _logsChannel.Dispose();
        _tracesChannel.Dispose();
    }

    public void Export(ExportLogsServiceRequest request)
    {
        _logsClient.Export(request, _headers);
    }

    public Task ExportAsync(ExportLogsServiceRequest request)
    {
        return _logsClient.ExportAsync(request, _headers).ResponseAsync;
    }
    
    public void Export(ExportTraceServiceRequest request)
    {
        _tracesClient.Export(request, _headers);
    }

    public Task ExportAsync(ExportTraceServiceRequest request)
    {
        return _tracesClient.ExportAsync(request, _headers).ResponseAsync;
    }
}
