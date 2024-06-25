﻿// Copyright © Serilog Contributors
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

using Serilog.Collections;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.OpenTelemetry.Configuration;
using Serilog.Sinks.OpenTelemetry.Exporters;
using System.Net.Http;

namespace Serilog;

/// <summary>
/// Adds OpenTelemetry sink configuration methods to <see cref="LoggerSinkConfiguration"/>.
/// </summary>
public static class OpenTelemetryLoggerConfigurationExtensions
{
    static HttpMessageHandler? CreateDefaultHttpMessageHandler() =>
#if FEATURE_SOCKETS_HTTP_HANDLER
        new SocketsHttpHandler { ActivityHeadersPropagator = null };
#else
        null;
#endif

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The `WriteTo` configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="ignoreEnvironment">If false the configuration will be overridden with values from <see href="https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/">OTLP Exporter Configuration environment variables</see>.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<BatchedOpenTelemetrySinkOptions> configure,
        bool ignoreEnvironment = false)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new BatchedOpenTelemetrySinkOptions();
        configure(options);

        if (!ignoreEnvironment)
        {
            OpenTelemetryEnvironment.Configure(options, Environment.GetEnvironmentVariable);
        }

        var exporter = Exporter.Create(
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            headers: new Dictionary<string, string>(options.Headers),
            httpMessageHandler: options.HttpMessageHandler ?? CreateDefaultHttpMessageHandler());

        var openTelemetrySink = new OpenTelemetrySink(
            exporter: exporter,
            formatProvider: options.FormatProvider,
            resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
            includedData: options.IncludedData);

        return loggerSinkConfiguration.Sink(openTelemetrySink, options.BatchingOptions, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The `WriteTo` configuration object.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP exporter endpoint.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use.
    /// </param>
    /// <param name="headers">
    /// Headers to send with network requests.
    /// </param>
    /// <param name="resourceAttributes">
    /// A attributes of the resource attached to the logs generated by the sink. The values must be simple primitive
    /// values: integers, doubles, strings, or booleans. Other values will be silently ignored.
    /// </param>
    /// <param name="includedData">
    /// Which fields should be included in the log events generated by the sink.
    /// </param>
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.
    /// </param>
    /// <param name="levelSwitch">
    /// A switch allowing the pass-through minimum level to be changed at runtime.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object>? resourceAttributes = null,
        IncludedData? includedData = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));

        return loggerSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            options.IncludedData = includedData ?? options.IncludedData;
            options.RestrictedToMinimumLevel = restrictedToMinimumLevel;
            options.LevelSwitch = levelSwitch;
            headers?.AddTo(options.Headers);
            resourceAttributes?.AddTo(options.ResourceAttributes);
        });
    }

    /// <summary>
    /// Audit to an OTLP exporter, waiting for each event to be acknowledged, and propagating errors to the caller.
    /// </summary>
    /// <param name="loggerAuditSinkConfiguration">
    /// The `AuditTo` configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerAuditSinkConfiguration loggerAuditSinkConfiguration,
        Action<OpenTelemetrySinkOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new OpenTelemetrySinkOptions();
        
        configure(options);

        var exporter = Exporter.Create(
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            headers: new Dictionary<string, string>(options.Headers),
            httpMessageHandler: options.HttpMessageHandler ?? CreateDefaultHttpMessageHandler());

        var sink = new OpenTelemetrySink(
            exporter: exporter,
            formatProvider: options.FormatProvider,
            resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
            includedData: options.IncludedData);

        return loggerAuditSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }
    
    /// <summary>
    /// Audit to an OTLP exporter, waiting for each event to be acknowledged, and propagating errors to the caller.
    /// </summary>
    /// <param name="loggerAuditSinkConfiguration">
    /// The `AuditTo` configuration object.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP exporter endpoint.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use.
    /// </param>
    /// <param name="headers">
    /// Headers to send with network requests.
    /// </param>
    /// <param name="resourceAttributes">
    /// A attributes of the resource attached to the logs generated by the sink. The values must be simple primitive
    /// values: integers, doubles, strings, or booleans. Other values will be silently ignored.
    /// </param>
    /// <param name="includedData">
    /// Which fields should be included in the log events generated by the sink.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerAuditSinkConfiguration loggerAuditSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object>? resourceAttributes = null,
        IncludedData? includedData = null)
    {
        if (loggerAuditSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerAuditSinkConfiguration));

        return loggerAuditSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            options.IncludedData = includedData ?? options.IncludedData;
            headers?.AddTo(options.Headers);
            resourceAttributes?.AddTo(options.ResourceAttributes);
        });
    }
}
