using Duende.IdentityModel.Client;
using LF.Application;
using LF.Infrastructure;
using LF.WebApi.Models.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// Configure logger
// https://github.com/serilog/serilog-aspnetcore
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

static void ConfigureServices(IServiceCollection services)
{
    services.AddAuthenticationApplication();
    // GrpcChannel rejects Aspire's https+http scheme; http:// works with service discovery.
    services.AddInfrastructureGrpcClient("http://lf-identityservice");
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
}

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();
    //builder.Configuration
    //    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

    var configuration = builder.Configuration;

    ConfigureOptions(builder.Services);

    var defaultAuth = configuration.GetSection(DefaultAuthOptions.SectionName).Get<DefaultAuthOptions>()
        ?? throw new InvalidOperationException("DefaultAuth configuration is missing.");
    var pmiAuth = configuration.GetSection(PmiAuthOptions.SectionName).Get<PmiAuthOptions>()
        ?? throw new InvalidOperationException("PmiAuth configuration is missing.");

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

            // TODO: change this
            options.Cookie.HttpOnly = false;

            options.LoginPath = new PathString("/login");
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;

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

            // save tokens
            options.SaveTokens = true;

            options.Events.OnAuthorizationCodeReceived = async (context) =>
            {
                //var request = context.HttpContext.Request;
                var redirectUri = context
                    .Properties
                    ?.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey] ?? "/";
                var code = context.ProtocolMessage.Code;

                using var client = new HttpClient();
                var discoResponsee = await client.GetDiscoveryDocumentAsync(options.Authority);

                var tokenResponse = await client.RequestAuthorizationCodeTokenAsync(new()
                {
                    Address = discoResponsee.TokenEndpoint,
                    ClientId = options.ClientId!,
                    ClientSecret = options.ClientSecret,
                    Code = code,
                    RedirectUri = redirectUri,
                });

                if (tokenResponse.IsError)
                {
                    // Error handler
                    throw new Exception("OpenIdConnect::Bad auth. Can't exchange code for access token and id token");
                }

                var accessToken = tokenResponse.AccessToken ?? string.Empty;
                var idToken = tokenResponse.IdentityToken ?? string.Empty;

                context.HandleCodeRedemption(accessToken, idToken);
            };

            options.MapInboundClaims = false;
            options.SignInScheme = defaultAuth.TempAuthCookieName;
        });

    // Configure application services
    ConfigureServices(builder.Services);

    var env = builder.Environment;

    // Add controllers to the container
    builder.Services
        .AddControllersWithViews();

    /* BUILD */
    var app = builder.Build();
    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline
    if (!env.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios
        // see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseStaticFiles();
    app.UseRouting();

    // https://habr.com/ru/articles/468401/
    app.UseAuthentication();
    app.UseAuthorization();

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
    Log.Fatal(ex, "LF.WebAPI application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
