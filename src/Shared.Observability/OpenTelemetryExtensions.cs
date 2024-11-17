using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shared.Observability.Options;

namespace Shared.Observability;

public static class OpenTelemetryExtensions
{
    // public static IServiceCollection AddAzureMonitor(this IServiceCollection services,
    //     IConfiguration configuration, OpenTelemetryBuilder otel)
    // {
    //     string? azureMonitorConnectionString = configuration.GetValue<string>("AzureMonitor:ConnectionString");
    //     if (!string.IsNullOrWhiteSpace(azureMonitorConnectionString))
    //     {
    //         otel.UseAzureMonitor();
    //     }
    //
    //     return services;
    // }


    public static void AddOpenTelemetryLogsInstrumentation(this ILoggingBuilder builder,
        IConfiguration configuration, Action<OpenTelemetryLoggerOptions>? configureLoggerOptions = null)
    {
        var otelOltpLogsOptions = configuration
            .GetSection(OtelOltpLogsOptions.ConfigSectionName).Get<OtelOltpLogsOptions>();

        builder.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            configureLoggerOptions?.Invoke(options);

            if (otelOltpLogsOptions?.ConsoleExporter ?? false)
                options.AddConsoleExporter();
        });
    }

    public static IServiceCollection AddOpenTelemetryMetricsInstrumentation(this IServiceCollection services,
        IConfiguration configuration, IOpenTelemetryBuilder otel,
        Action<MeterProviderBuilder>? configureMeterProviderBuilder = null)
    {
        otel.WithMetrics(meterBuilder =>
        {
            var otelOltpMetricsOptions = configuration
                .GetSection(OtelOltpMetricsOptions.ConfigSectionName).Get<OtelOltpMetricsOptions>();

            meterBuilder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // Metrics provides by ASP.NET Core
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel");

            configureMeterProviderBuilder?.Invoke(meterBuilder);

            if (otelOltpMetricsOptions?.ConsoleExporter ?? false)
                meterBuilder.AddConsoleExporter();
        });

        return services;
    }

    public static IServiceCollection AddOpenTelemetryTracingInstrumentation(this IServiceCollection services,
        IConfiguration configuration, IOpenTelemetryBuilder otel,
        Action<TracerProviderBuilder>? configureTracerProviderBuilder = null)
    {
        otel.WithTracing(tracing =>
        {
            var otelOltpTracingOptions = configuration
                .GetSection(OtelOltpTracingOptions.ConfigSectionName).Get<OtelOltpTracingOptions>();

            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();

            configureTracerProviderBuilder?.Invoke(tracing);

            if (otelOltpTracingOptions?.ConsoleExporter ?? false)
                tracing.AddConsoleExporter();
        });

        return services;
    }

    public static IServiceCollection UseOpenTelemetryOltpExporter(
        this IServiceCollection services, IConfiguration configuration, IOpenTelemetryBuilder otel)
    {
        // Export OpenTelemetry data via OTLP, using env vars for the configuration
        string? otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            otel.UseOtlpExporter();
        }

        return services;
    }
}