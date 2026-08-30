using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using LF.WebApi.Models.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
        IWebHostEnvironment environment,
        LFAppAuth.IAuthenticationService authenticationService,
        LFAppAuth.ITokenService tokenService) : ControllerBase
    {
        private readonly ILogger<AuthController> _logger = logger;
        private readonly IWebHostEnvironment _environment = environment;

        private readonly DefaultAuthOptions _authOptions = authOptions.Value;
        private readonly PmiAuthOptions _pmiAuthOptions = pmiAuthOptions.Value;
        private readonly GoogleAuthOptions _googleAuthOptions = googleAuthOptions.Value;

        private readonly LFAppAuth.IAuthenticationService _authenticationService = authenticationService;
        private readonly LFAppAuth.ITokenService _tokenService = tokenService;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignInPmi()
        {
            _logger.LogInformation("AuthController::SignInPmi: Start PMI Club authentication");

            var schemeName = _pmiAuthOptions.SchemeName;
            var props = new AuthenticationProperties
            {
                RedirectUri = new PathString(_pmiAuthOptions.RedirectUri),
                Items =
                {
                    { "scheme", schemeName }
                }
            };

            _logger.LogInformation("AuthController::SignInPmi: Start Oidc challenge with scheme name {schemeName}", schemeName);
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
            _logger.LogInformation("AuthController::{actionName}: Start authentication callback", actionName);

            // Read the outcome of external auth
            var authResult = await HttpContext.AuthenticateAsync(_authOptions.TempAuthCookieName);

            if (!authResult.Succeeded)
            {
                _logger.LogError("AuthController::{actionName}: Can't read the outcome of external authentication", actionName);
                return LocalRedirect(new PathString("/"));
            }

            try
            {
                var userAuthDto = ParseUserAuthDto(authResult.Principal);
                var userDto = await authenticateAsync(userAuthDto);
                _logger.LogInformation("AuthController::{actionName}: Authenticated user {usrEmail} {usrFirstName}",
                    actionName, userDto.Email, userDto.FirstName);

                await IssueSessionCookieAsync(userDto);

                _logger.LogInformation("AuthController::{actionName}: Success SignIn user", actionName);
                return LocalRedirect(new PathString("/courses"));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "AuthController::{actionName}: Can't authentificate user", actionName);
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
            // SignOut from temp cookie
            await HttpContext.SignOutAsync(_authOptions.TempAuthCookieName);
        }
    }
}
