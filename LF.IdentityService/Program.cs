using Lf.IdentityService.Services;
using LF.Application;
using LF.Infrastructure;
using LF.Infrastructure.Persistence.Seed;
using Mapster;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddUserApplication();
builder.Services.AddInfrastructureDatabase(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.MapGrpcService<RpcUserService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
