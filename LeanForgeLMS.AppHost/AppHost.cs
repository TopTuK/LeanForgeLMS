var builder = DistributedApplication.CreateBuilder(args);

// Set by the "payment-check" launch profile. Starts an ngrok tunnel to lf-webapi so Robokassa's
// ResultURL webhook and Success/Fail redirects can reach the local machine.
var tunnelEnabled = string.Equals(builder.Configuration["LF_TUNNEL"], "true", StringComparison.OrdinalIgnoreCase);

var pgUser = builder.AddParameter("postgres-user", "leanforge");
var pgPassword = builder.AddParameter("postgres-password", "leanforge", secret: true);
var postgres = builder
    .AddPostgres("postgres", port: 5432)
    .WithEnvironment("POSTGRES_USER", "leanforge") // for scripts
    .WithEnvironment("POSTGRES_PASSWORD", "leanforge") // for scripts
    .WithEnvironment("POSTGRES_DB", "leanforge") // for scripts
    .WithUserName(pgUser)
    .WithPassword(pgPassword)
    .WithDataVolume("leanforge-db")
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

// Unleash client API token for the feature flag server. Only lf-webapi needs it — the gRPC
// services never evaluate flags. Empty when unset, which leaves every flag off.
var unleashApiKey = builder.AddParameter(
    "unleash-api-key",
    () => builder.Configuration["UNLEASH_API_KEY"] ?? string.Empty,
    secret: true);

// SMTP settings for lf-notificationservice. Empty when unset, which leaves email dispatch
// disabled (queued emails stay Pending) rather than failing every delivery attempt.
var smtpHost = builder.AddParameter("smtp-host", () => builder.Configuration["SMTP_HOST"] ?? string.Empty);
var smtpUserName = builder.AddParameter("smtp-username", () => builder.Configuration["SMTP_USERNAME"] ?? string.Empty);
var smtpPassword = builder.AddParameter(
    "smtp-password",
    () => builder.Configuration["SMTP_PASSWORD"] ?? string.Empty,
    secret: true);
var smtpFromAddress = builder.AddParameter(
    "smtp-from-address",
    () => builder.Configuration["SMTP_FROM_ADDRESS"] ?? builder.Configuration["SMTP_USERNAME"] ?? string.Empty);

// The toolkit defaults to docker.io/minio/minio, which MinIO withdrew from Docker Hub
// (2026-09-11). Same image and tag, served from quay.io — MinIO's official registry.
var minio = builder
    .AddMinioContainer("minio", minioUser, minioPassword, port: 9000)
    .WithImageRegistry("quay.io")
    .WithDataVolume("leanforge-minio-data");

var identityService = builder
    .AddProject<Projects.Lf_IdentityService>("lf-identityservice")
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

// Delivers the LFEmailMessages outbox. Plain HTTP/1.1 (no gRPC), so its /health works with
// Aspire's probe. WaitForStart on identityService: that host applies migrations but its
// Http2-only /health endpoint would make WaitFor hang.
builder
    .AddProject<Projects.LF_NotificationService>("lf-notificationservice")
    .WithEnvironment("SENTRY_DSN", sentryDsn)
    .WithEnvironment("Smtp__Host", smtpHost)
    .WithEnvironment("Smtp__UserName", smtpUserName)
    .WithEnvironment("Smtp__Password", smtpPassword)
    .WithEnvironment("Smtp__FromAddress", smtpFromAddress)
    .WithReference(postgres)
    .WaitFor(postgres)
    .WaitForStart(identityService);

var webApp = builder
    .AddViteApp("lf-webapp", "../lf.webapp")
    .WithNpm()
    .WithHttpEndpoint(port: 5173, env: "PORT");

var webApi = builder
    // Pin the launch profile explicitly: LF.WebApi/Properties/launchSettings.json declares "http"
    // before "https", and Aspire's AddProject defaults to the first "Project" profile in the file.
    // Without this, lf-webapi never gets an HTTPS endpoint under Aspire, every request (including
    // the PMI/Google/Yandex OIDC/OAuth challenge redirect_uri) is plaintext, and those providers
    // reject the http:// redirect_uri ("External authentication does not support http").
    .AddProject<Projects.LF_WebApi>("lf-webapi", launchProfileName: "https")
    .WithEnvironment("SENTRY_DSN", sentryDsn)
    .WithEnvironment("Unleash__ApiKey", unleashApiKey)
    .WithReference(identityService)
    .WithReference(courseService)
    .WithReference(paymentService)
    .WithReference(webApp)
    .WaitFor(webApp)
    .WithReference(minio)
    .WaitFor(minio)
    .WithReference(postgres)
    .WaitFor(postgres);

if (tunnelEnabled)
{
    // The gRPC hosts are Http2-only, so their /health endpoint rejects Aspire's HTTP/1.1 readiness
    // probe and WaitFor(...) would hang lf-webapi forever. Wait for Running instead of Healthy.
    webApi
        .WaitForStart(identityService)
        .WaitForStart(courseService)
        .WaitForStart(paymentService);

    var ngrokAuthToken = builder.AddParameter(
        "ngrok-auth-token",
        () => builder.Configuration["NGROK_AUTHTOKEN"]
              ?? throw new InvalidOperationException(
                  "The payment-check launch profile needs an ngrok auth token: "
                  + "dotnet user-secrets set NGROK_AUTHTOKEN <token> --project LeanForgeLMS.AppHost"),
        secret: true);

    builder
        .AddNgrok("ngrok", endpointPort: 4040) // fixed inspector port: http://localhost:4040
        .WithAuthToken(ngrokAuthToken)
        .WithTunnelEndpoint(webApi, "http"); // ephemeral public domain -> lf-webapi http endpoint
}
else
{
    webApi
        .WaitFor(identityService)
        .WaitFor(courseService)
        .WaitFor(paymentService);
}

builder
    .Build()
    .Run();
