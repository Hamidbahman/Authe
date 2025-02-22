using Authentication.Application;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Authentication.Api.Controllers;



    [Route("api/oauth")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly OAuthService _oauthService;
        private readonly RecaptchaService _recaptchaService;

        public OAuthController(OAuthService oauthService, RecaptchaService recaptchaService)
        {
            _oauthService = oauthService;
            _recaptchaService = recaptchaService;
        }



        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizeApplication([FromForm] string clientId, [FromForm] string clientSecret)
        {
            var authCode = await _oauthService.GenerateAuthorizationCodeAsync(clientId, clientSecret);
            if (authCode == null)
                return Unauthorized(new { message = "Invalid client credentials or application restrictions." });

            return Ok(new { authenticationCode = authCode });
        }

        /// <summary>
        /// Handles user login with 2FA and reCAPTCHA verification if required.
        /// </summary>
        [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var tokens = await _oauthService.ValidateAndGenerateTokensAsync(
                request.Username, request.Password, request.AuthenticationCode);

            if (tokens == null)
                return Unauthorized(new { message = "Invalid login credentials." });

            return Ok(new 
            { 
                accessToken = tokens.Value.accessToken, 
                refreshToken = tokens.Value.refreshToken 
            });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
       
    // DTO for Login Request
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthenticationCode {get;set;}
        public string? RecaptchaResponse { get; set; } // Optional (only sent when required)
        public string? TwoFactorCode { get; set; } // Optional (only sent when required)
    }
}
