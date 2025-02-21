using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Auth.Controllers
{
    [Route("api/oauth")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly OAuthService _oauthService;

        public OAuthController(OAuthService oauthService)
        {
            _oauthService = oauthService;
        }

        /// <summary>
        /// Generates an authorization code for a given clientId and clientSecret.
        /// </summary>
        [HttpPost("generate-auth-code")]
        public async Task<IActionResult> GenerateAuthorizationCode([FromBody] AuthCodeRequest request)
        {
            var authCode = await _oauthService.GenerateAuthorizationCodeAsync(request.ClientId, request.ClientSecret);
            if (authCode == null)
                return Unauthorized("Invalid client credentials or policy restriction.");

            return Ok(new { AuthorizationCode = authCode });
        }

        /// <summary>
        /// Validates authentication code, user credentials, and generates access/refresh tokens.
        /// </summary>
        [HttpPost("validate-and-generate-tokens")]
        public async Task<IActionResult> ValidateAndGenerateTokens([FromBody] TokenRequest request)
        {
            var tokens = await _oauthService.ValidateAndGenerateTokensAsync(
                request.ClientId, request.Username, request.Password, request.AuthenticationCode);

            if (tokens == null)
                return Unauthorized("Invalid credentials or authentication code.");

            return Ok(new { AccessToken = tokens.Value.accessToken, RefreshToken = tokens.Value.refreshToken });
        }
    }

    // DTOs for request payloads
    public class AuthCodeRequest
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class TokenRequest
    {
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthenticationCode { get; set; }
    }
}
