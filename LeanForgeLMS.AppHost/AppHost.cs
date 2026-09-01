var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("postgres-user", "leanforge");
var pgPassword = builder.AddParameter("postgres-password", "leanforge", secret: true);
var postgres = builder
    .AddPostgres("postgres", port: 5432)
    .WithEnvironment("POSTGRES_USER", "leanforge") // for scripts
    .WithEnvironment("POSTGRES_PASSWORD", "leanforge") // for scripts
    .WithEnvironment("POSTGRES_DB", "leanforge") // for scripts
    .WithUserName(pgUser)
    .WithPassword(pgPassword)
    //.WithDataVolume("leanforge-db") // for debug do not save datavolume
    .AddDatabase("leanforge");

var minioUser = builder.AddParameter("minio-user", "minioadmin");
var minioPassword = builder.AddParameter("minio-password", "minioadmin", secret: true);

// Sentry DSN forwarded to every service as SENTRY_DSN (the Sentry SDK reads that env var
// natively). Resolves from AppHost configuration/user-secrets; empty when unset, which
// leaves Sentry disabled for that session.
var sentryDsn = builder.AddParameter(
    "sentry-dsn",
    () => builder.Configuration["SENTRY_DSN"] ?? string.Empty,
    secret: true);

var minio = builder.AddMinioContainer("minio", minioUser, minioPassword, port: 9000);

var identityService = builder
    .AddProject<Projects.LF_IdentityService>("lf-identityservice")
    .WithEnvironment("SENTRY_DSN", sentryDsn)
    .WithReference(postgres)
    .WaitFor(postgres);

var courseService = builder
    .AddProject<Projects.LF_CourseService>("lf-courseservice")
    .WithEnvironment("SENTRY_DSN", sentryDsn)
    .WithReference(postgres)
    .WaitFor(postgres);

var paymentService = builder
    .AddProject<Projects.LF_PaymentService>("lf-paymentservice")
    .WithEnvironment("SENTRY_DSN", sentryDsn)
    .WithReference(postgres)
    .WaitFor(postgres);

var webApp = builder
    .AddViteApp("lf-webapp", "../lf.webapp")
    .WithNpm()
    .WithHttpEndpoint(port: 5173, env: "PORT");

builder
    .AddProject<Projects.LF_WebApi>("lf-webapi")
    .WithEnvironment("SENTRY_DSN", sentryDsn)
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithReference(courseService)
    .WaitFor(courseService)
    .WithReference(paymentService)
    .WaitFor(paymentService)
    .WithReference(webApp)
    .WaitFor(webApp)
    .WithReference(minio)
    .WaitFor(minio)
    .WithReference(postgres)
    .WaitFor(postgres);

builder
    .Build()
    .Run();
