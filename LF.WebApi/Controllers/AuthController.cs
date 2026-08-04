using LF.Application.ModelDto.Authentication;
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
        LFAppAuth.IAuthenticationService authenticationService,
        LFAppAuth.ITokenService tokenService) : ControllerBase
    {
        private readonly ILogger<AuthController> _logger = logger;
        
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
        {
            _logger.LogInformation("AuthController::SingInPmiCallback: Start authentication callback");

            // Read the outcome of external auth
            _logger.LogInformation("AuthController::SingInPmiCallback: begin read external authentication");
            var authResult = await HttpContext.AuthenticateAsync(_authOptions.TempAuthCookieName);

            if (!authResult.Succeeded)
            {
                _logger.LogError("AuthController::SingInPmiCallback: Can't read the outcome of external authentication");
                return LocalRedirect(new PathString("/"));
            }

            try
            {
                string? firstName = null, lastName = string.Empty;
                var name = authResult.Principal
                    .Claims
                    .FirstOrDefault(c => c.Type == "name")?
                    .Value;
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

                var userAuthDto = new UserAuthentificationDto
                {
                    Sub = authResult.Principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                    Email = authResult.Principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                    FirstName = firstName,
                    LastName = lastName,
                };
                
                var userDto = await _authenticationService.AuthenticatePmiUserAsync(userAuthDto);
                _logger.LogInformation("AuthController::SingInPmiCallback: Authenticated user {usrEmail} {usrFirstName}",
                    userDto.Email, userDto.FirstName);

                // Create JWT token
                var claims = new List<Claim>()
                {
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
                            MaxAge = TimeSpan.FromDays(_authOptions.AuthMaxAgeDays)
                        }
                    );
                // SignOut from temp cookie
                await HttpContext.SignOutAsync(_authOptions.TempAuthCookieName);

                _logger.LogInformation("AuthController::SinginCallback: Success SignIn user");
                return LocalRedirect(new PathString("/courses"));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "AuthController::SingInPmiCallback: Can't authentificate user");
                return LocalRedirect(new PathString("/"));
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("AuthController::Logout: start logout user");

            HttpContext.Response
                .Cookies
                .Delete(_authOptions.AuthCookieName);

            return await Task.FromResult(LocalRedirect(new PathString("/")));
        }
    }
}
