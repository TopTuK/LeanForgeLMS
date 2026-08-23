using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Reflection;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, OpenTelemetry, and Serilog console logging.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    // Shared by the bootstrap logger (below) and the full logger (AddServiceDefaults) so console output
    // never diverges in appearance between the two Serilog stages.
    private const string ConsoleOutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] ({Application}) {Message:lj}{NewLine}{Exception}";

    // Stage 1 of Serilog's two-stage setup (https://github.com/serilog/serilog-aspnetcore) — captures
    // errors that happen before the DI container is available. Call before WebApplication.CreateBuilder,
    // so IHostEnvironment isn't available yet; the app name is derived from the entry assembly instead,
    // matching what ASP.NET Core uses for IHostEnvironment.ApplicationName by default.
    public static void CreateBootstrapLogger()
    {
        var applicationName = Assembly.GetEntryAssembly()!.GetName().Name!;

        Log.Logger = new LoggerConfiguration()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: ConsoleOutputTemplate)
            .CreateBootstrapLogger();
    }

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // ClearProviders must run before ConfigureOpenTelemetry (which adds the OpenTelemetry logging
        // provider) — otherwise it would wipe that provider too and silently break the Aspire dashboard's
        // log view. It only removes the default Console/Debug/EventSource providers CreateBuilder adds.
        builder.Logging.ClearProviders();

        // Stage 2 — the full logger, with DI services and configuration available.
        builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            // AddSerilog() bypasses the standard Logging:LogLevel filtering pipeline for its own provider,
            // so appsettings.json's "Microsoft.AspNetCore": "Warning" override no longer applies — replicate
            // it here to keep framework request/routing noise out of the console.
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: ConsoleOutputTemplate));

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    // Collapses ASP.NET Core's/gRPC's default multi-line-per-request logging into one structured summary
    // line. Health/liveness polling is demoted to Verbose (below the default Information minimum) so it
    // doesn't spam the console — those paths are polled frequently in Development.
    public static WebApplication UseDefaultRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, _) =>
                httpContext.Request.Path.StartsWithSegments(HealthEndpointPath)
                || httpContext.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information;
        });

        return app;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
