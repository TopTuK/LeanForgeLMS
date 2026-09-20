using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using LF.WebApi.Models.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using LFAppAuth = LF.Application.Services.Authentication;

namespace LF.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController(ILogger<AuthController> logger,
        IOptionsSnapshot<DefaultAuthOptions> authOptions,
        IOptionsSnapshot<PmiAuthOptions> pmiAuthOptions,
        IOptionsSnapshot<GoogleAuthOptions> googleAuthOptions,
        IOptionsSnapshot<YandexAuthOptions> yandexAuthOptions,
        IWebHostEnvironment environment,
        LFAppAuth.IAuthenticationService authenticationService,
        LFAppAuth.ITokenService tokenService) : ControllerBase
    {
        private readonly ILogger<AuthController> _logger = logger;
        private readonly IWebHostEnvironment _environment = environment;

        private readonly DefaultAuthOptions _authOptions = authOptions.Value;
        private readonly PmiAuthOptions _pmiAuthOptions = pmiAuthOptions.Value;
        private readonly GoogleAuthOptions _googleAuthOptions = googleAuthOptions.Value;
        private readonly YandexAuthOptions _yandexAuthOptions = yandexAuthOptions.Value;

        private readonly LFAppAuth.IAuthenticationService _authenticationService = authenticationService;
        private readonly LFAppAuth.ITokenService _tokenService = tokenService;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignInPmi()
        {
            _logger.LogInformation(
                "AuthController::SignInPmi: Start PMI Club authentication for {remoteIp}, user agent {userAgent}, referer {referer}, existing session cookie {hasSessionCookie}",
                HttpContext.Connection.RemoteIpAddress,
                Request.Headers.UserAgent.ToString(),
                Request.Headers.Referer.ToString(),
                Request.Cookies.ContainsKey(_authOptions.AuthCookieName));

            var schemeName = _pmiAuthOptions.SchemeName;
            var props = new AuthenticationProperties
            {
                RedirectUri = new PathString(_pmiAuthOptions.RedirectUri),
                Items =
                {
                    { "scheme", schemeName }
                }
            };

            _logger.LogInformation(
                "AuthController::SignInPmi: Start Oidc challenge with scheme name {schemeName}, returning to {redirectUri}",
                schemeName, _pmiAuthOptions.RedirectUri);
            return Challenge(props, schemeName);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SingInPmiCallback()
            => await HandleExternalSignInCallbackAsync(
                "SingInPmiCallback",
                _authenticationService.AuthenticatePmiUserAsync);

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignInGoogle()
        {
            _logger.LogInformation("AuthController::SignInGoogle: Start Google authentication");

            var schemeName = _googleAuthOptions.SchemeName;
            var props = new AuthenticationProperties
            {
                RedirectUri = new PathString(_googleAuthOptions.RedirectUri),
                Items =
                {
                    { "scheme", schemeName }
                }
            };

            _logger.LogInformation("AuthController::SignInGoogle: Start Oidc challenge with scheme name {schemeName}", schemeName);
            return Challenge(props, schemeName);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SignInGoogleCallback()
            => await HandleExternalSignInCallbackAsync(
                "SignInGoogleCallback",
                _authenticationService.AuthenticateGoogleUserAsync);

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignInYandex()
        {
            _logger.LogInformation("AuthController::SignInYandex: Start Yandex authentication");

            var schemeName = _yandexAuthOptions.SchemeName;
            var props = new AuthenticationProperties
            {
                RedirectUri = new PathString(_yandexAuthOptions.RedirectUri),
                Items =
                {
                    { "scheme", schemeName }
                }
            };

            _logger.LogInformation("AuthController::SignInYandex: Start OAuth challenge with scheme name {schemeName}", schemeName);
            return Challenge(props, schemeName);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SignInYandexCallback()
            => await HandleExternalSignInCallbackAsync(
                "SignInYandexCallback",
                _authenticationService.AuthenticateYandexUserAsync);

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("AuthController::Logout: start logout user");

            HttpContext.Response
                .Cookies
                .Delete(_authOptions.AuthCookieName, new CookieOptions { Path = "/" });

            return await Task.FromResult(LocalRedirect(new PathString("/")));
        }

        private async Task<IActionResult> HandleExternalSignInCallbackAsync(
            string actionName,
            Func<UserAuthentificationDto, Task<UserDto>> authenticateAsync)
        {
            var startedAt = Stopwatch.GetTimestamp();

            _logger.LogInformation(
                "AuthController::{actionName}: Start authentication callback from {remoteIp}, temp cookie {hasTempCookie}, user agent {userAgent}",
                actionName,
                HttpContext.Connection.RemoteIpAddress,
                Request.Cookies.ContainsKey(_authOptions.TempAuthCookieName),
                Request.Headers.UserAgent.ToString());

            // Read the outcome of external auth
            var authResult = await HttpContext.AuthenticateAsync(_authOptions.TempAuthCookieName);

            if (!authResult.Succeeded)
            {
                // The temp cookie is written by the external handler and read back here: a miss means
                // the external sign-in never completed, or the cookie didn't survive the round-trip.
                _logger.LogError(authResult.Failure,
                    "AuthController::{actionName}: Can't read the outcome of external authentication (temp scheme {tempScheme}, cookie present {hasTempCookie}, failure {failureMessage}). Redirecting to /",
                    actionName, _authOptions.TempAuthCookieName,
                    Request.Cookies.ContainsKey(_authOptions.TempAuthCookieName),
                    authResult.Failure?.Message ?? "<none>");
                return LocalRedirect(new PathString("/"));
            }

            try
            {
                var userAuthDto = ParseUserAuthDto(authResult.Principal);

                if (userAuthDto.Sub is null || userAuthDto.Email is null)
                {
                    // Downstream lookup keys: log which claims actually arrived so a provider that
                    // changed its claim shape is obvious from the logs alone.
                    _logger.LogWarning(
                        "AuthController::{actionName}: External principal is missing claims (sub present {hasSub}, email present {hasEmail}, name present {hasName}); received claim types {claimTypes}",
                        actionName, userAuthDto.Sub is not null, userAuthDto.Email is not null,
                        userAuthDto.FirstName is not null,
                        string.Join(',', authResult.Principal.Claims.Select(c => c.Type).Distinct()));
                }

                _logger.LogInformation(
                    "AuthController::{actionName}: External sign-in resolved for subject {usrSub}, authentication scheme {authScheme}",
                    actionName, userAuthDto.Sub, authResult.Ticket?.AuthenticationScheme);

                var userDto = await authenticateAsync(userAuthDto);
                _logger.LogInformation("AuthController::{actionName}: Authenticated user {usrEmail} {usrFirstName}",
                    actionName, userDto.Email, userDto.FirstName);

                await IssueSessionCookieAsync(userDto);

                _logger.LogInformation(
                    "AuthController::{actionName}: Success SignIn user {usrId} with role {usrRole} in {elapsedMs} ms. Redirecting to /courses",
                    actionName, userDto.Id, userDto.Role,
                    Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds));
                return LocalRedirect(new PathString("/courses"));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "AuthController::{actionName}: Can't authentificate user after {elapsedMs} ms. Redirecting to /",
                    actionName, Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds));
                return LocalRedirect(new PathString("/"));
            }
        }

        private static UserAuthentificationDto ParseUserAuthDto(ClaimsPrincipal principal)
        {
            string? firstName = null, lastName = string.Empty;
            var name = principal.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            if (name is not null)
            {
                var splitName = name.Split(' ');
                firstName = splitName[0];
                lastName = string.Empty;

                if (splitName.Length > 1)
                {
                    lastName = splitName[1].Trim();
                }
            }

            return new UserAuthentificationDto
            {
                Sub = principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                Email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                FirstName = firstName,
                LastName = lastName,
            };
        }

        private async Task IssueSessionCookieAsync(UserDto userDto)
        {
            var claims = new List<Claim>()
            {
                new(ClaimTypes.NameIdentifier, userDto.Id.ToString()),
                new("email", userDto.Email),
                new("role", userDto.Role.ToString())
            };
            var jwtToken = _tokenService.CreateWebJwtToken(claims, new()
            {
                Issuer = _authOptions.JwtIssuer,
                Audience = _authOptions.JwtAudience,
                Key = _authOptions.JwtKey,
                ExpiresDays = _authOptions.JwtExpiresDays,
            });

            HttpContext.Response
                .Cookies
                .Append(
                    key: _authOptions.AuthCookieName,
                    value: jwtToken.Token,
                    options: new CookieOptions()
                    {
                        MaxAge = TimeSpan.FromDays(_authOptions.AuthMaxAgeDays),
                        HttpOnly = true,
                        Secure = !_environment.IsDevelopment(),
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                    }
                );
            _logger.LogInformation(
                "AuthController::IssueSessionCookie: Issued session cookie {cookieName} for user {usrId} (secure {isSecure}, max age {maxAgeDays} days, JWT expires in {jwtExpiresDays} days)",
                _authOptions.AuthCookieName, userDto.Id, !_environment.IsDevelopment(),
                _authOptions.AuthMaxAgeDays, _authOptions.JwtExpiresDays);

            // SignOut from temp cookie
            await HttpContext.SignOutAsync(_authOptions.TempAuthCookieName);
        }
    }
}
