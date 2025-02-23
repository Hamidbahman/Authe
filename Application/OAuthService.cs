// using System;
// using System.Collections.Concurrent;
// using System.Security.Authentication;
// using System.Security.Cryptography;
// using System.Text;
// using System.Threading.Tasks;
// using Authentication.Domain.Entities;
// using Authentication.Domain.Enums;
// using Authentication.Domain.Repositories;

// namespace Authentication.Application
// {
//     public class OAuthService
//     {
//         private readonly IApplicationRepository _applicationRepository;
//         private readonly IUserRepository _userRepo;
//         private readonly RecaptchaService _recaptchaService;
//         private readonly OTPService _otpService;
//         private static readonly ConcurrentDictionary<string, string> _authCodes = new();

//         public OAuthService(
//             OTPService otpService,
//             RecaptchaService recaptchaService,
//             IApplicationRepository applicationRepository,
//             IUserRepository userRepository)
//         {
//             _recaptchaService = recaptchaService;
//             _applicationRepository = applicationRepository;
//             _userRepo = userRepository;
//             _otpService = otpService;
//         }

//         public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret)
//         {
//             var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
//             if (application == null || clientSecret != application.ClientSecret)
//                 return null;

//             var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
//             if (configLock?.CaptchaNeeded == true)
//                 return null;

//             string authCode = Guid.NewGuid().ToString();
//             _authCodes.TryAdd(authCode, clientId);
//             return authCode;
//         }

//         public async Task<(bool IsSuccess, bool RequiresRecaptcha, bool RequiresTwoFactor, (string accessToken, string refreshToken)? Tokens)> 
//         LoginAsync(string username, string password, string authenticationCode, string? recaptchaResponse = null, string? twoFactorCode = null)
//         {
//             if (!_authCodes.TryRemove(authenticationCode, out _))
//                 throw new AuthenticationException("Invalid or expired authentication code.");

//             var user = await _userRepo.GetByUsernameAsync(username);
//             if (user == null)
//                 return (false, false, false, null);

//             var loginPolicy = await _userRepo.GetLoginPoliciesByUserID(user.Id.ToString());
//             if (loginPolicy != null)
//             {
//                 switch (loginPolicy.LockTypes)
//                 {
//                     case LockTypes.TemporaryLock:
//                         throw new AuthenticationException("Your account is temporarily locked. Try again later.");
//                     case LockTypes.PermanentLock:
//                         throw new AuthenticationException("Your account has been permanently locked. Please contact support.");
//                     case LockTypes.ExpiringLock:
//                         throw new AuthenticationException("Your account is locked due to security reasons. Try again later.");
//                     case LockTypes.ConditionalLock:
//                         throw new AuthenticationException("Additional verification is required to access your account.");
//                 }
//             }

//             // Secure password verification
//             if (!VerifyHashedPassword(user.UserProperty.Password, password))
//             {
//                 user.IncrementLoginAttempt();
//                 await _userRepo.SaveChangesAsync();

//                 if (user.LoginAttempt > 5)
//                 {
//                     loginPolicy?.SetLockType(LockTypes.TemporaryLock);
//                     await _userRepo.SaveChangesAsync();
//                 }

//                 if (user.LoginAttempt < 5)
//                 {
//                     bool isHuman = await _recaptchaService.ValidateRecaptchaAsync(recaptchaResponse ?? "");
//                     if (!isHuman)
//                         return (false, true, false, null);
//                 }
//                 return (false, false, false, null);
//             }

//             if (user.TwoFactorEnabled && string.IsNullOrEmpty(twoFactorCode))
//                 return (false, false, true, null);

//             if (user.TwoFactorEnabled)
//             {
//                 bool isOtpValid = await _otpService.ValidateTwoFactorCodeAsync(user.PhoneNumber, twoFactorCode!);
//                 if (!isOtpValid)
//                     return (false, false, true, null);
//             }

//             user.ResetLoginAttempt();
//             await _userRepo.SaveChangesAsync();

//             string accessToken = TokenService.GenerateAccessToken(user);
//             string refreshToken = TokenService.GenerateRefreshToken();
//             return (true, false, false, (accessToken, refreshToken));
//         }

//         public async Task<string> ValidateAndGenerateTokensAsync(string otpCode, string phoneNumber)
//         {
//             var user = await _userRepo.GetUserByPhoneNumber(phoneNumber);
//             if (user == null)
//                 throw new UnauthorizedAccessException("Invalid phone number.");

//             bool isOtpValid = await _otpService.ValidateTwoFactorCodeAsync(phoneNumber, otpCode);
//             if (!isOtpValid)
//                 throw new UnauthorizedAccessException("Invalid OTP.");

//             return TokenService.GenerateAccessToken(user);
//         }

//         private bool VerifyHashedPassword(string hashedPassword, string providedPassword)
//         {
//             using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(providedPassword)))
//             {
//                 string computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(providedPassword)));
//                 return computedHash == hashedPassword;
//             }
//         }
//     }

//     public static class TokenService
//     {
//         private const int AccessTokenExpiryMinutes = 30;

//         public static string GenerateAccessToken(User user)
//         {
//             string payload = $"{user.Username}:{user.Id}:{DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes):O}";
//             return GenerateHmacToken(payload, user.Id.ToString());
//         }

//         public static string GenerateRefreshToken()
//         {
//             return GenerateSecureRandomToken();
//         }

//         private static string GenerateHmacToken(string data, string secret)
//         {
//             using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
//             {
//                 byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
//                 return Convert.ToBase64String(hash);
//             }
//         }

//         private static string GenerateSecureRandomToken()
//         {
//             byte[] tokenBytes = new byte[32];
//             using (var rng = RandomNumberGenerator.Create())
//             {
//                 rng.GetBytes(tokenBytes);
//             }
//             return Convert.ToBase64String(tokenBytes);
//         }
//     }
// }


using System;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Repositories;

namespace Authentication.Application
{
    public class OAuthService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepo;
        private readonly OTPService _otpService;
        private readonly RecaptchaService _recaptchaService;
        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(
            RecaptchaService recaptchaService,
            OTPService otpService,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository)
        {
            _recaptchaService = recaptchaService;
            _applicationRepository = applicationRepository;
            _userRepo = userRepository;
            _otpService = otpService;
        }

        public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret)
        {
            var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
            if (application == null || clientSecret != application.ClientSecret)
                return null;
            
            var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
            
            if(configLock.CaptchaNeeded == true)
            {
                throw new AuthenticationException("CaptchaNeeded");
                
            }



            string authCode = Guid.NewGuid().ToString();
            _authCodes.TryAdd(authCode, clientId);
            return authCode;
        }

        public async Task<(string accessToken, string refreshToken)> LoginAsync(string username, string password, string authenticationCode)
        {


            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
            {
                throw new ArgumentNullException("No User found by this username");
            }


            var loginPolicy = await _userRepo.GetLoginPoliciesByUserID(user.Id.ToString());
            if (loginPolicy != null)
            {
                switch (loginPolicy.LockTypes)
                {
                    case LockTypes.TemporaryLock:
                        throw new AuthenticationException("Your account is temporarily locked. Try again later.");
                    case LockTypes.PermanentLock:
                        throw new AuthenticationException("Your account has been permanently locked. Please contact support.");
                    case LockTypes.ExpiringLock:
                        throw new AuthenticationException("Your account is locked due to security reasons. Try again later.");
                    case LockTypes.ConditionalLock:
                        throw new AuthenticationException("Additional verification is required to access your account.");
                }
            }

            // Secure password verification
            if (!VerifyHashedPassword(user.UserProperty.Password, password))
            {
                user.IncrementLoginAttempt();
                await _userRepo.SaveChangesAsync();

                if (user.LoginAttempt > 5)
                {
                    loginPolicy?.SetLockType(LockTypes.TemporaryLock);
                    await _userRepo.SaveChangesAsync();
                }
            }


            string token = TokenService.GenerateAccessToken(user);
            string refreshToken = TokenService.GenerateRefreshToken();

            return (token, refreshToken);

            // if (user.TwoFactorEnabled && string.IsNullOrEmpty(twoFactorCode))
            //     return (false, true, null);

            // if (user.TwoFactorEnabled)
            // {
            //     bool isOtpValid = await _otpService.ValidateTwoFactorCodeAsync(user.PhoneNumber, twoFactorCode!);
            //     if (!isOtpValid)
            //         return (false, true, null);
            // }

            // user.ResetLoginAttempt();
            // await _userRepo.SaveChangesAsync();

            // string accessToken = TokenService.GenerateAccessToken(user);
            // string refreshToken = TokenService.GenerateRefreshToken();
            // return (true, false, (accessToken, refreshToken));
        }


        // public async Task<string> ValidateAndGenerateTokensAsync(string otpCode, string phoneNumber)
        // {
        //     var user = await _userRepo.GetUserByPhoneNumber(phoneNumber);
        //     if (user == null)
        //         throw new UnauthorizedAccessException("Invalid phone number.");

        //     bool isOtpValid = await _otpService.ValidateTwoFactorCodeAsync(phoneNumber, otpCode);
        //     if (!isOtpValid)
        //         throw new UnauthorizedAccessException("Invalid OTP.");

        //     return TokenService.GenerateAccessToken(user);
        // }

        private bool VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(providedPassword)))
            {
                string computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(providedPassword)));
                return computedHash == hashedPassword;
            }
        }
    }

    public static class TokenService
    {
        private const int AccessTokenExpiryMinutes = 30;

        public static string GenerateAccessToken(User user)
        {
            string payload = $"{user.Username}:{user.Id}:{DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes):O}";
            return GenerateHmacToken(payload, user.Id.ToString());
        }

        public static string GenerateRefreshToken()
        {
            return GenerateSecureRandomToken();
        }

        private static string GenerateHmacToken(string data, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }

        private static string GenerateSecureRandomToken()
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }
    }
}