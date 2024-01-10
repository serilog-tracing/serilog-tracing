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

// ReSharper disable PossibleMultipleEnumeration

using System.Globalization;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Sinks.OpenTelemetry.Formatting;
using SerilogTracing.Sinks.OpenTelemetry.ProtocolHelpers;

namespace SerilogTracing.Sinks.OpenTelemetry;

static class OtlpEventBuilder
{
    public static (LogRecord logRecord, string? scopeName) ToLogRecord(LogEvent logEvent, IFormatProvider? formatProvider, IncludedData includedData)
    {
        var logRecord = new LogRecord();

        ProcessProperties(logRecord.Attributes.Add, logEvent, includedData, out var scopeName);
        ProcessTimestamp(logRecord, logEvent);
        ProcessMessage((message) => { logRecord.Body = new AnyValue {StringValue = message}; }, logEvent, includedData, formatProvider);
        ProcessLevel(logRecord, logEvent);
        ProcessException(logRecord.Attributes, logEvent);
        ProcessIncludedFields(logRecord, logEvent, includedData);

        return (logRecord, scopeName);
    }
    
    public static (Span span, string? scopeName) ToSpan(LogEvent logEvent, IFormatProvider? formatProvider, IncludedData includedData)
    {
        var span = new Span();
        
        ProcessProperties(span.Attributes.Add, logEvent, includedData, out var scopeName);
        ProcessTimestamp(span, logEvent);
        ProcessStartTime(span, logEvent);
        ProcessMessage((message) => { span.Name = message; }, logEvent, includedData, formatProvider);
        ProcessLevel(span, logEvent);
        ProcessException(span.Attributes, logEvent);
        ProcessIncludedFields(span, logEvent, includedData);

        return (span, scopeName);
    }

    public static void ProcessMessage(Action<string> setBody, LogEvent logEvent, IncludedData includedFields, IFormatProvider? formatProvider)
    {
        if (!includedFields.HasFlag(IncludedData.TemplateBody))
        {
            var renderedMessage = CleanMessageTemplateFormatter.Format(logEvent.MessageTemplate, logEvent.Properties, formatProvider);

            if (renderedMessage.Trim() != "")
            {
                setBody(renderedMessage);
            }
        }
        else if (includedFields.HasFlag(IncludedData.TemplateBody) && logEvent.MessageTemplate.Text.Trim() != "")
        {
            setBody(logEvent.MessageTemplate.Text);
        }
    }

    public static void ProcessLevel(LogRecord logRecord, LogEvent logEvent)
    {
        var level = logEvent.Level;
        logRecord.SeverityText = level.ToString();
        logRecord.SeverityNumber = PrimitiveConversions.ToSeverityNumber(level);
    }
    
    public static void ProcessLevel(Span span, LogEvent logEvent)
    {
        span.Status = PrimitiveConversions.ToStatus(logEvent.Level);
    }
    
    public static void ProcessProperties(Action<KeyValue> adder, LogEvent logEvent, IncludedData includedData, out string? scopeName)
    {
        scopeName = null;
        foreach (var property in logEvent.Properties)
        {
            if (property is { Key: Constants.SourceContextPropertyName, Value: ScalarValue { Value: string sourceContext } })
            {
                scopeName = sourceContext;
                if ((includedData & IncludedData.SourceContextAttribute) != IncludedData.SourceContextAttribute)
                {
                    continue;
                }
            }

            if (property is {Key: SerilogTracing.Core.Constants.SpanStartTimestampPropertyName})
            {
                continue;
            }

            var v = PrimitiveConversions.ToOpenTelemetryAnyValue(property.Value);
            adder(PrimitiveConversions.NewAttribute(property.Key, v));
        }
    }

    public static void ProcessTimestamp(LogRecord logRecord, LogEvent logEvent)
    {
        logRecord.TimeUnixNano = PrimitiveConversions.ToUnixNano(logEvent.Timestamp);
        logRecord.ObservedTimeUnixNano = logRecord.TimeUnixNano;
    }
    
    public static void ProcessTimestamp(Span span, LogEvent logEvent)
    {
        span.EndTimeUnixNano = PrimitiveConversions.ToUnixNano(logEvent.Timestamp);
    }

    static void ProcessStartTime(Span span, LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue(SerilogTracing.Core.Constants.SpanStartTimestampPropertyName,
                out var sst) && sst is ScalarValue {Value: DateTime start})
        {
            span.StartTimeUnixNano = PrimitiveConversions.ToUnixNano(start);
        }
    }

    public static void ProcessException(RepeatedField<KeyValue> attrs, LogEvent logEvent)
    {
        var ex = logEvent.Exception;
        if (ex != null)
        {
            attrs.Add(PrimitiveConversions.NewStringAttribute(SemanticConventions.AttributeExceptionType, ex.GetType().ToString()));

            if (ex.Message != "")
            {
                attrs.Add(PrimitiveConversions.NewStringAttribute(SemanticConventions.AttributeExceptionMessage, ex.Message));
            }

            if (ex.ToString() != "")
            {
                attrs.Add(PrimitiveConversions.NewStringAttribute(SemanticConventions.AttributeExceptionStacktrace, ex.ToString()));
            }
        }
    }

    static void ProcessIncludedFields(LogRecord logRecord, LogEvent logEvent, IncludedData includedFields)
    {
        if ((includedFields & IncludedData.TraceIdField) != IncludedData.None && logEvent.TraceId is {} traceId)
        {
            logRecord.TraceId = PrimitiveConversions.ToOpenTelemetryTraceId(traceId.ToHexString());
        }

        if ((includedFields & IncludedData.SpanIdField) != IncludedData.None && logEvent.SpanId is {} spanId)
        {
            logRecord.SpanId = PrimitiveConversions.ToOpenTelemetrySpanId(spanId.ToHexString());
        }

        if ((includedFields & IncludedData.MessageTemplateTextAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(PrimitiveConversions.NewAttribute(SemanticConventions.AttributeMessageTemplateText, new()
            {
                StringValue = logEvent.MessageTemplate.Text
            }));
        }

        if ((includedFields & IncludedData.MessageTemplateMD5HashAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(PrimitiveConversions.NewAttribute(SemanticConventions.AttributeMessageTemplateMD5Hash, new()
            {
                StringValue = PrimitiveConversions.Md5Hash(logEvent.MessageTemplate.Text)
            }));
        }

        if ((includedFields & IncludedData.MessageTemplateRenderingsAttribute) != IncludedData.None)
        {
            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

            // Better not to allocate an array in the 99.9% of cases where this is false
            if (tokensWithFormat.Any())
            {
                var renderings = new ArrayValue();

                foreach (var propertyToken in tokensWithFormat)
                {
                    var space = new StringWriter();
                    propertyToken.Render(logEvent.Properties, space, CultureInfo.InvariantCulture);
                    renderings.Values.Add(new AnyValue { StringValue = space.ToString() });
                }
                
                logRecord.Attributes.Add(PrimitiveConversions.NewAttribute(
                    SemanticConventions.AttributeMessageTemplateRenderings,
                    new AnyValue { ArrayValue = renderings }));
            }
        }
    }
    
    static void ProcessIncludedFields(Span span, LogEvent logEvent, IncludedData includedFields)
    {
        if ((includedFields & IncludedData.TraceIdField) != IncludedData.None && logEvent.TraceId is {} traceId)
        {
            span.TraceId = PrimitiveConversions.ToOpenTelemetryTraceId(traceId.ToHexString());
        }

        if ((includedFields & IncludedData.SpanIdField) != IncludedData.None && logEvent.SpanId is {} spanId)
        {
            span.SpanId = PrimitiveConversions.ToOpenTelemetrySpanId(spanId.ToHexString());
        }

        if ((includedFields & IncludedData.MessageTemplateTextAttribute) != IncludedData.None)
        {
            span.Attributes.Add(PrimitiveConversions.NewAttribute(SemanticConventions.AttributeMessageTemplateText, new()
            {
                StringValue = logEvent.MessageTemplate.Text
            }));
        }

        if ((includedFields & IncludedData.MessageTemplateMD5HashAttribute) != IncludedData.None)
        {
            span.Attributes.Add(PrimitiveConversions.NewAttribute(SemanticConventions.AttributeMessageTemplateMD5Hash, new()
            {
                StringValue = PrimitiveConversions.Md5Hash(logEvent.MessageTemplate.Text)
            }));
        }

        if ((includedFields & IncludedData.MessageTemplateRenderingsAttribute) != IncludedData.None)
        {
            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

            // Better not to allocate an array in the 99.9% of cases where this is false
            if (tokensWithFormat.Any())
            {
                var renderings = new ArrayValue();

                foreach (var propertyToken in tokensWithFormat)
                {
                    var space = new StringWriter();
                    propertyToken.Render(logEvent.Properties, space, CultureInfo.InvariantCulture);
                    renderings.Values.Add(new AnyValue { StringValue = space.ToString() });
                }
                
                span.Attributes.Add(PrimitiveConversions.NewAttribute(
                    SemanticConventions.AttributeMessageTemplateRenderings,
                    new AnyValue { ArrayValue = renderings }));
            }
        }
    }
}
