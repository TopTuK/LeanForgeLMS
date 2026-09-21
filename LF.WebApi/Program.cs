using AspNet.Security.OAuth.Yandex;
using Duende.IdentityModel.Client;
using LF.AppDomain.Models.User.Enums;
using LF.Application;
using LF.Infrastructure;
using LF.WebApi.Common;
using LF.WebApi.Endpoints;
using LF.WebApi.Models.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NetEscapades.AspNetCore.SecurityHeaders;
using Sentry;
using Serilog;
using System.Security.Claims;
using System.Text;

// Configure logger
// https://github.com/serilog/serilog-aspnetcore
Extensions.CreateBootstrapLogger();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddAuthenticationApplication();
    // GrpcChannel rejects Aspire's https+http scheme; http:// works with service discovery.
    services.AddInfrastructureGrpcClient("http://lf-identityservice");
    services.AddInfrastructureCourseGrpcClient("http://lf-courseservice");
    services.AddInfrastructurePaymentGrpcClient("http://lf-paymentservice");
    services.AddInfrastructureFileStorage(configuration);
    // Unleash-backed feature flags. LF.WebApi is the only host that registers these: the gRPC
    // services run on an egress-less internal network and cannot reach the Unleash server.
    services.AddInfrastructureFeatureFlags(configuration);
    // Needed for IStorageService/IStorageRepository (StorageObject metadata) — LF.WebApi is the only
    // host with both MinIO and DB access, since a course-cover/lesson-media upload needs both in one call.
    services.AddInfrastructureDatabase(configuration);
}

static void MapEndpointGroups(WebApplication app)
{
    var groups = typeof(Program).Assembly
        .GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IEndpointGroup).IsAssignableFrom(t))
        .Select(t => (IEndpointGroup)Activator.CreateInstance(t)!);

    foreach (var group in groups)
    {
        group.Map(app);
    }
}

static void ConfigureOptions(IServiceCollection services)
{
    services.AddOptions<DefaultAuthOptions>()
        .BindConfiguration(DefaultAuthOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddOptions<PmiAuthOptions>()
        .BindConfiguration(PmiAuthOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddOptions<GoogleAuthOptions>()
        .BindConfiguration(GoogleAuthOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddOptions<YandexAuthOptions>()
        .BindConfiguration(YandexAuthOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddOptions<DevAuthOptions>()
        .BindConfiguration(DevAuthOptions.SectionName);
}

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();
    builder.AddMinioClient("minio");
    //builder.Configuration
    //    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

    var configuration = builder.Configuration;

    ConfigureOptions(builder.Services);

    var defaultAuth = configuration.GetSection(DefaultAuthOptions.SectionName).Get<DefaultAuthOptions>()
        ?? throw new InvalidOperationException("DefaultAuth configuration is missing.");
    var pmiAuth = configuration.GetSection(PmiAuthOptions.SectionName).Get<PmiAuthOptions>()
        ?? throw new InvalidOperationException("PmiAuth configuration is missing.");
    var googleAuth = configuration.GetSection(GoogleAuthOptions.SectionName).Get<GoogleAuthOptions>()
        ?? throw new InvalidOperationException("GoogleAuth configuration is missing.");
    var yandexAuth = configuration.GetSection(YandexAuthOptions.SectionName).Get<YandexAuthOptions>()
        ?? throw new InvalidOperationException("YandexAuth configuration is missing.");

    /* ADD AUTHENTICATION */
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie("LfAuthCookie", options =>
        {
            options.Cookie.Name = defaultAuth.AuthCookieName;
            options.ExpireTimeSpan = TimeSpan.FromDays(defaultAuth.AuthMaxAgeDays);

            // The session JWT must never be readable by page scripts — the SPA authenticates by
            // sending this cookie (withCredentials), not by copying the token into a header.
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.LoginPath = new PathString("/login");
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;

            // The JWT is delivered to the browser in an HttpOnly cookie, so accept it from there
            // as well as the Authorization header (which API clients and tests still use).
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token)
                        && context.Request.Cookies.TryGetValue(defaultAuth.AuthCookieName, out var cookieToken)
                        && !string.IsNullOrEmpty(cookieToken))
                    {
                        context.Token = cookieToken;
                    }

                    return Task.CompletedTask;
                },
            };

            // https://metanit.com/sharp/aspnet6/13.2.php
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Validate publisher (issuer) of token
                ValidateIssuer = true,
                ValidIssuer = defaultAuth.JwtIssuer,

                // Validate consumer (audience) of token
                ValidateAudience = true,
                ValidAudience = defaultAuth.JwtAudience,

                // Validate lifetime of token
                ValidateLifetime = true,

                // Validate signature key
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(defaultAuth.JwtKey)),
                ValidateIssuerSigningKey = true,
            };
        })
        .AddCookie(defaultAuth.TempAuthCookieName)
        .AddOpenIdConnect(pmiAuth.SchemeName, options =>
        {
            // OpenIdConfigurationUrl is the full discovery document URL; Authority is the issuer base.
            var metadataUri = new Uri(pmiAuth.OpenIdConfigurationUrl);
            options.MetadataAddress = pmiAuth.OpenIdConfigurationUrl;
            options.Authority = $"{metadataUri.Scheme}://{metadataUri.Authority}";
            options.ClientId = pmiAuth.ClientId;
            options.ClientSecret = pmiAuth.ClientSecret;

            // Set the callback path, so it will call back to.
            options.CallbackPath = new PathString(pmiAuth.CallbackPath);

            // Set response type to code
            options.ResponseType = OpenIdConnectResponseType.Code;

            // Configure the scope
            options.Scope.Clear();
            options.Scope.Add("openid");

            // Nothing reads the PMI tokens back — AuthController only needs the id_token claims. Saving
            // them roughly doubled the temp sign-in cookie, and the /auth/signin-oidc response headers
            // then overflowed nginx-proxy's default proxy_buffer_size, so the browser got a 502 in place
            // of the redirect to SingInPmiCallback (works locally, where there is no proxy).
            options.SaveTokens = false;

            // The handlers below other than OnAuthorizationCodeReceived only log. {Instance} is the
            // container hostname, so callbacks served by a second lf-webapi instance stand out.
            options.Events.OnRedirectToIdentityProvider = context =>
            {
                var clock = PmiOidcDiagnostics.Clock(context.HttpContext);
                PmiOidcDiagnostics.MarkChallengeStarted(context.Properties, clock);

                var message = context.ProtocolMessage;
                var pkceMethod = message.Parameters.TryGetValue("code_challenge_method", out var method) ? method : "none";
                PmiOidcDiagnostics.Logger(context.HttpContext).LogInformation(
                    "PmiOidc::Challenge: Redirecting to {AuthorizationEndpoint} (client {ClientId}, redirect_uri {RedirectUri}, response_mode {ResponseMode}, PKCE {PkceMethod}) on {Instance}",
                    message.IssuerAddress, message.ClientId, message.RedirectUri, message.ResponseMode,
                    pkceMethod, Environment.MachineName);

                return Task.CompletedTask;
            };

            options.Events.OnMessageReceived = context =>
            {
                var message = context.ProtocolMessage;
                var request = context.HttpContext.Request;
                var logger = PmiOidcDiagnostics.Logger(context.HttpContext);

                if (!string.IsNullOrEmpty(message.Error))
                {
                    logger.LogWarning(
                        "PmiOidc::Callback: PMI returned error {Error} - {ErrorDescription} ({Method} from {RemoteIp}) on {Instance}",
                        message.Error, message.ErrorDescription, request.Method,
                        context.HttpContext.Connection.RemoteIpAddress, Environment.MachineName);
                }
                else
                {
                    // The handler decrypts the state into Properties and clears ProtocolMessage.State
                    // before this event, so Properties — not State — tells us the state was valid.
                    logger.LogInformation(
                        "PmiOidc::Callback: Received {Method} with code {CodeFingerprint} (length {CodeLength}), state resolved {HasState}, from {RemoteIp}, user agent {UserAgent} on {Instance}",
                        request.Method, PmiOidcDiagnostics.Fingerprint(message.Code), message.Code?.Length ?? 0,
                        context.Properties is not null, context.HttpContext.Connection.RemoteIpAddress,
                        request.Headers.UserAgent.ToString(), Environment.MachineName);
                }

                return Task.CompletedTask;
            };

            options.Events.OnAuthorizationCodeReceived = async (context) =>
            {
                var logger = PmiOidcDiagnostics.Logger(context.HttpContext);
                var clock = PmiOidcDiagnostics.Clock(context.HttpContext);

                //var request = context.HttpContext.Request;
                var redirectUri = context
                    .Properties
                    ?.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey] ?? "/";
                var code = context.ProtocolMessage.Code;
                var codeFingerprint = PmiOidcDiagnostics.Fingerprint(code);

                logger.LogInformation(
                    "PmiOidc::CodeExchange: Start for code {CodeFingerprint}, {ElapsedSinceChallengeMs} ms after the challenge, redirect_uri {RedirectUri} on {Instance}",
                    codeFingerprint, PmiOidcDiagnostics.MillisecondsSinceChallenge(context.Properties, clock),
                    redirectUri, Environment.MachineName);

                using var client = new HttpClient();

                var discoveryStartedAt = clock.GetTimestamp();
                var discoResponsee = await client.GetDiscoveryDocumentAsync(options.Authority);
                var discoveryMs = Math.Round(clock.GetElapsedTime(discoveryStartedAt).TotalMilliseconds);

                if (discoResponsee.IsError)
                {
                    logger.LogError(
                        "PmiOidc::CodeExchange: Discovery document from {Authority} failed after {DiscoveryMs} ms: {DiscoveryError} ({DiscoveryErrorType}, HTTP {DiscoveryStatus})",
                        options.Authority, discoveryMs, discoResponsee.Error, discoResponsee.ErrorType, (int)discoResponsee.HttpStatusCode);
                }
                else
                {
                    logger.LogInformation(
                        "PmiOidc::CodeExchange: Discovery document loaded in {DiscoveryMs} ms, token endpoint {TokenEndpoint}",
                        discoveryMs, discoResponsee.TokenEndpoint);
                }

                var tokenRequest = new AuthorizationCodeTokenRequest
                {
                    Address = discoResponsee.TokenEndpoint,
                    ClientId = options.ClientId!,
                    ClientSecret = options.ClientSecret,
                    Code = code,
                    RedirectUri = redirectUri,
                };

                // PKCE: the challenge request included a code_challenge (options.UsePkce defaults to true),
                // so the token endpoint requires the matching code_verifier or it rejects the exchange.
                var hasCodeVerifier = false;
                if (context.Properties?.Items.TryGetValue(OAuthConstants.CodeVerifierKey, out var codeVerifier) is true
                    && codeVerifier is not null)
                {
                    tokenRequest.Parameters.Add(OAuthConstants.CodeVerifierKey, codeVerifier);
                    hasCodeVerifier = true;
                }

                var tokenStartedAt = clock.GetTimestamp();
                var tokenResponse = await client.RequestAuthorizationCodeTokenAsync(tokenRequest);
                var tokenMs = Math.Round(clock.GetElapsedTime(tokenStartedAt).TotalMilliseconds);

                if (tokenResponse.IsError)
                {
                    logger.LogError(
                        "PmiOidc::CodeExchange: Token request for code {CodeFingerprint} failed after {TokenMs} ms ({ElapsedSinceChallengeMs} ms after the challenge): HTTP {TokenStatus}, {TokenError} - {TokenErrorDescription} ({TokenErrorType}); code_verifier sent {HasCodeVerifier}, redirect_uri {RedirectUri} on {Instance}",
                        codeFingerprint, tokenMs, PmiOidcDiagnostics.MillisecondsSinceChallenge(context.Properties, clock),
                        (int)tokenResponse.HttpStatusCode, tokenResponse.Error, tokenResponse.ErrorDescription, tokenResponse.ErrorType,
                        hasCodeVerifier, redirectUri, Environment.MachineName);

                    // Error handler
                    throw new Exception(
                        $"OpenIdConnect::Bad auth. Can't exchange code for access token and id token: {tokenResponse.Error} - {tokenResponse.ErrorDescription}");
                }

                logger.LogInformation(
                    "PmiOidc::CodeExchange: Token request for code {CodeFingerprint} succeeded in {TokenMs} ms (id_token {HasIdToken}, access_token {HasAccessToken}, expires in {ExpiresIn} s)",
                    codeFingerprint, tokenMs, !string.IsNullOrEmpty(tokenResponse.IdentityToken),
                    !string.IsNullOrEmpty(tokenResponse.AccessToken), tokenResponse.ExpiresIn);

                var accessToken = tokenResponse.AccessToken ?? string.Empty;
                var idToken = tokenResponse.IdentityToken ?? string.Empty;

                context.HandleCodeRedemption(accessToken, idToken);
            };

            options.Events.OnTokenValidated = context =>
            {
                PmiOidcDiagnostics.Logger(context.HttpContext).LogInformation(
                    "PmiOidc::TokenValidated: id_token accepted for subject {Subject}",
                    context.Principal?.FindFirst("sub")?.Value);

                return Task.CompletedTask;
            };

            options.Events.OnAuthenticationFailed = context =>
            {
                PmiOidcDiagnostics.Logger(context.HttpContext).LogError(context.Exception,
                    "PmiOidc::AuthenticationFailed: {ExceptionType} on {Instance}",
                    context.Exception.GetType().Name, Environment.MachineName);

                return Task.CompletedTask;
            };

            // Logs only; without HandleResponse() the handler still surfaces the failure as before.
            options.Events.OnRemoteFailure = context =>
            {
                PmiOidcDiagnostics.Logger(context.HttpContext).LogError(
                    "PmiOidc::RemoteFailure: {FailureType}: {FailureMessage} ({ElapsedSinceChallengeMs} ms after the challenge) on {Instance}",
                    context.Failure?.GetType().Name, context.Failure?.Message,
                    PmiOidcDiagnostics.MillisecondsSinceChallenge(context.Properties,
                        PmiOidcDiagnostics.Clock(context.HttpContext)),
                    Environment.MachineName);

                return Task.CompletedTask;
            };

            options.MapInboundClaims = false;
            options.SignInScheme = defaultAuth.TempAuthCookieName;
        })
        .AddGoogle(googleAuth.SchemeName, options =>
        {
            options.ClientId = googleAuth.ClientId;
            options.ClientSecret = googleAuth.ClientSecret;

            // Set the callback path, so it will call back to.
            options.CallbackPath = new PathString(googleAuth.CallbackPath);

            // save tokens
            options.SaveTokens = true;

            // Google's default ClaimActions map to the long ClaimTypes.* URIs; remap to the short
            // "sub"/"email"/"name" claim types PMI's OIDC handler produces, so the shared claim-parsing
            // logic in AuthController doesn't need to special-case providers.
            options.ClaimActions.Clear();
            options.ClaimActions.MapJsonKey("sub", "sub");
            options.ClaimActions.MapJsonKey("email", "email");
            options.ClaimActions.MapJsonKey("name", "name");

            options.SignInScheme = defaultAuth.TempAuthCookieName;
        })
        .AddYandex(yandexAuth.SchemeName, options =>
        {
            options.ClientId = yandexAuth.ClientId;
            options.ClientSecret = yandexAuth.ClientSecret;

            // Set the callback path, so it will call back to.
            options.CallbackPath = new PathString(yandexAuth.CallbackPath);

            // save tokens
            options.SaveTokens = true;

            // Yandex only returns the account email / real name when these scopes are requested.
            options.Scope.Add("login:email");
            options.Scope.Add("login:info");

            // Yandex's default ClaimActions map to the long ClaimTypes.* URIs; remap to the short
            // "sub"/"email"/"name" claim types PMI's OIDC handler produces, so the shared claim-parsing
            // logic in AuthController doesn't need to special-case providers. "real_name" is
            // "First Last" (matching how AuthController splits the name claim); "display_name" is
            // the fallback when the account has no real name set.
            options.ClaimActions.Clear();
            options.ClaimActions.MapJsonKey("sub", "id");
            options.ClaimActions.MapJsonKey("email", "default_email");
            options.ClaimActions.MapJsonKey("name", "real_name");
            options.ClaimActions.MapJsonKey("name", "display_name");

            options.SignInScheme = defaultAuth.TempAuthCookieName;
        });

    // Additive on top of the auth scheme wiring above. The JWT is issued with a literal "role" claim,
    // but JwtBearerHandler's default inbound claim mapping (MapInboundClaims=true, left at its default
    // for this scheme) remaps "role" -> ClaimTypes.Role by the time the ClaimsPrincipal is built for a
    // request, so the policy must check ClaimTypes.Role here, not the literal "role" string.
    builder.Services
        .AddAuthorizationBuilder()
        .AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, nameof(UserRole.Admin)))
        .AddPolicy("CourseCreatorOrAdmin", policy => policy.RequireClaim(ClaimTypes.Role, nameof(UserRole.Instructor), nameof(UserRole.CourseCreator), nameof(UserRole.Admin)));

    // Configure application services
    ConfigureServices(builder.Services, configuration);

    var env = builder.Environment;

    // Add controllers to the container
    builder.Services
        .AddControllersWithViews();

    /* BUILD */
    var app = builder.Build();
    app.MapDefaultEndpoints();
    app.UseDefaultRequestLogging();

    // Configure the HTTP request pipeline
    if (!env.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios
        // see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        app.UseHttpsRedirection();

        // Prod-only (dev is served through the Vite proxy, which needs a far looser policy).
        // The CSP is the defence-in-depth backstop for the sanitized rich-text render path.
        app.UseSecurityHeaders(policies => policies
            .AddDefaultSecurityHeaders()
            .AddContentSecurityPolicy(csp =>
            {
                csp.AddDefaultSrc().Self();
                csp.AddScriptSrc().Self().From("https://www.googletagmanager.com"); // GA4 gtag.js, loaded only after consent
                csp.AddStyleSrc().Self().UnsafeInline();          // Vue scoped styles + :style bindings + Vuestic
                csp.AddImgSrc().Self().Data().From("blob:").From("https:");
                csp.AddFontSrc().Self();
                csp.AddConnectSrc().Self()
                    .From("https://*.google-analytics.com")
                    .From("https://*.analytics.google.com")
                    .From("https://*.googletagmanager.com");
                csp.AddBaseUri().Self();
                csp.AddFormAction().Self();
                csp.AddFrameAncestors().None();
                csp.AddObjectSrc().None();
                csp.AddUpgradeInsecureRequests();
            }));
    }

    app.UseStaticFiles();
    app.UseRouting();

    // https://habr.com/ru/articles/468401/
    app.UseAuthentication();
    app.UseAuthorization();

    MapEndpointGroups(app);

#pragma warning disable ASP0014 // Suggest using top level route registrations
    app.UseEndpoints(ep =>
    {
        ep.MapControllerRoute(
            name: "default",
            pattern: "api/{controller}/{action=Index}/{id?}"
        );
    });
#pragma warning restore ASP0014 // Suggest using top level route registrations

    if (env.IsDevelopment())
    {
        // "services:lf-webapp:http:0" is injected by Aspire's WithReference(webApp) in AppHost.cs.
        // Falls back to Vite's default dev port when running LF.WebApi standalone (without Aspire).
        var webAppUri = configuration["services:lf-webapp:http:0"] ?? "http://localhost:5173";

        app.UseSpa(spa =>
        {
            spa.UseProxyToSpaDevelopmentServer(webAppUri);
        });
    }
    else
    {
        app.MapFallbackToFile("index.html");
    }

    /* RUN APP */
    app.Run();
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
    Log.Fatal(ex, "LF.WebAPI application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
