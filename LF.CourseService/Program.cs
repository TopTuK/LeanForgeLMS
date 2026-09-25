using LF.Application;
using LF.Application.Common.Options;
using LF.CourseService.Services;
using LF.Infrastructure;
using Mapster;
using Sentry;
using Serilog;

Microsoft.Extensions.Hosting.Extensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

    // Add services to the container.
    builder.Services.AddGrpc();
    builder.Services.AddCourseApplication();
    builder.Services.AddInfrastructureDatabase(builder.Configuration);

    // Links in enrollment confirmation emails. Fails at startup rather than queueing emails with broken links.
    builder.Services.AddOptions<AppUrlOptions>()
        .BindConfiguration(AppUrlOptions.SectionName)
        .Validate(options => options.IsValid, "App:PublicBaseUrl must be an absolute http(s) URL of the SPA.")
        .ValidateOnStart();

    var app = builder.Build();
    app.UseDefaultRequestLogging();

    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline.
    // Note: gRPC status codes live in HTTP/2 trailers, not the HTTP status code the request-logging
    // middleware reads, so a failed RPC still shows up as "200" in the summary line above.
    app.MapGrpcService<RpcCourseService>();
    app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

    app.Run();
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
    Log.Fatal(ex, "LF.CourseService application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
