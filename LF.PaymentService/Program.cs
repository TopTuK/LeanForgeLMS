using LF.Application;
using LF.Infrastructure;
using LF.PaymentService.Services;
using Mapster;
using Sentry;
using Serilog;

Microsoft.Extensions.Hosting.Extensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

    builder.Services.AddGrpc();
    builder.Services.AddPaymentApplication();
    builder.Services.AddInfrastructureDatabase(builder.Configuration);
    builder.Services.AddInfrastructureRobokassa(builder.Configuration);

    var app = builder.Build();
    app.UseDefaultRequestLogging();

    app.MapDefaultEndpoints();

    // gRPC status codes live in HTTP/2 trailers, not the HTTP status code the request-logging
    // middleware reads, so a failed RPC still shows up as "200" in the summary line above.
    app.MapGrpcService<RpcPaymentService>();
    app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

    app.Run();
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
    Log.Fatal(ex, "LF.PaymentService application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
