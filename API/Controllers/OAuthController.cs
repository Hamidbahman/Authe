// using System.Threading.Tasks;
// using Authentication.Application;
// using Microsoft.AspNetCore.Mvc;

// namespace Authentication.API.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class OAuthController : ControllerBase
//     {
//         private readonly OAuthService _authService;

//         public OAuthController(OAuthService authService)
//         {
//             _authService = authService;
//         }

//         [HttpPost("authorize")]
//         public async Task<IActionResult> GenerateAuthorizationCode([FromBody] AuthRequestDto request)
//         {
//             var authCode = await _authService.GenerateAuthorizationCodeAsync(request.ClientId, request.ClientSecret);
//             if (authCode == null)
//                 return Unauthorized(new { message = "Invalid client credentials." });

//             return Ok(new { authorizationCode = authCode });
//         }

//         [HttpPost("login")]
//         public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
//         {
//             var (isSuccess, requiresRecaptcha, requiresTwoFactor, tokens) = 
//                 await _authService.LoginAsync(request.Username, request.Password, request.AuthorizationCode, request.RecaptchaResponse, request.TwoFactorCode);

//             if (!isSuccess)
//             {
//                 if (requiresRecaptcha)
//                     return BadRequest(new { message = "Recaptcha required.", requiresRecaptcha = true });

//                 if (requiresTwoFactor)
//                     return BadRequest(new { message = "Two-factor authentication required.", requiresTwoFactor = true });

//                 return Unauthorized(new { message = "Invalid credentials." });
//             }

//             return Ok(new { accessToken = tokens?.accessToken, refreshToken = tokens?.refreshToken });
//         }

//         [HttpPost("validate-otp")]
//         public async Task<IActionResult> ValidateOtp([FromBody] OtpValidationRequestDto request)
//         {
//             try
//             {
//                 var accessToken = await _authService.ValidateAndGenerateTokensAsync(request.OtpCode, request.PhoneNumber);
//                 return Ok(new { accessToken });
//             }
//             catch (UnauthorizedAccessException ex)
//             {
//                 return Unauthorized(new { message = ex.Message });
//             }
//         }
//     }
// }


//     public class AuthRequestDto
//     {
//         public string ClientId { get; set; } = string.Empty;
//         public string ClientSecret { get; set; } = string.Empty;
//     }

//     public class LoginRequestDto
//     {
//         public string Username { get; set; } = string.Empty;
//         public string Password { get; set; } = string.Empty;
//         public string AuthorizationCode { get; set; } = string.Empty;
//         public string? RecaptchaResponse { get; set; }
//         public string? TwoFactorCode { get; set; }
//     }

//     public class OtpValidationRequestDto
//     {
//         public string OtpCode { get; set; } = string.Empty;
//         public string PhoneNumber { get; set; } = string.Empty;
//     }

using System.Security.Authentication;
using System.Threading.Tasks;
using Authentication.Application;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly OAuthService _authService;

        public OAuthController(OAuthService authService)
        {
            _authService = authService;
        }

[HttpPost("authorize")]
public async Task<IActionResult> GenerateAuthorizationCode([FromBody] AuthRequestDto request)
{
    try
    {
        var authCode = await _authService.GenerateAuthorizationCodeAsync(request.ClientId, request.ClientSecret);
        
        if (authCode == null)
            return Unauthorized(new { message = "Invalid client credentials." });

        return Ok(new { authorizationCode = authCode });
    }
    catch (AuthenticationException ex) when (ex.Message == "CaptchaNeeded")
    {
        return BadRequest(new { message = "CAPTCHA required.", requiresCaptcha = true });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
    }
}


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request.Username, request.Password, request.AuthorizationCode);


            return Ok(new { accessToken = result.accessToken, refreshToken = result.refreshToken });
        }
    }
}

public class AuthRequestDto
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
}
