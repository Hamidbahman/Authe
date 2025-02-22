using System;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Repositories;

namespace Authentication.Application
{
    public class OAuthService 
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepo;
        private readonly RecaptchaService _recaptchService;

        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(RecaptchaService recaptchaService, IApplicationRepository applicationRepository, IUserRepository userRepository )
        {
            _recaptchService = recaptchaService;
            _applicationRepository = applicationRepository;
            _userRepo = userRepository;
        }

public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret)
{
    var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
    if (application == null || clientSecret != application.ClientScope)
        return null;

    var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
    if (configLock != null && configLock.CaptchaNeeded==true)
        return null;

    string authCode = Guid.NewGuid().ToString();

    _authCodes.TryAdd(authCode, clientId);


    return authCode;
}

public async Task<(string accessToken, string refreshToken)?> ValidateAndGenerateTokensAsync(
    string username, string password, string authenticationCode)
{
    if (!_authCodes.TryRemove(authenticationCode, out _))
        throw new AuthenticationException("Invalid or expired authentication code.");

    var user = await _userRepo.GetByUsernameAsync(username);
    if (user == null)
        throw new AuthenticationException("Username or password is incorrect.");

    // Check for existing lock before processing login
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

    // Validate Credentials
    if (username != user.Username || password != user.UserProperty.Password)
    {
        user.IncrementLoginAttempt();
        await _userRepo.SaveChangesAsync(); 

        if (user.LoginAttempt > 5)
        {
            var logPolicy = await _userRepo.GetLoginPoliciesByUserID(user.Id.ToString());
            if (logPolicy != null)
            {
                logPolicy.SetLockType(LockTypes.TemporaryLock);
                await _userRepo.SaveChangesAsync();
            }
        }
        if (user.LoginAttempt < 5)
        {
            string recaptchaResponse = "";
            bool isHuman = await _recaptchService.ValidateRecaptchaAsync(recaptchaResponse);
            if(!isHuman)
                throw new AuthenticationException("reCaptch verification failed");

        }

        throw new AuthenticationException("Username or password is incorrect.");
    }
    if(username == user.Username || password == user.UserProperty.Password)
    {
        if(user.TwoFactorEnabled == true)
        {}
    }

    // Reset login attempts on successful login
    if (user.LoginAttempt > 0)
    {
        user.ResetLoginAttempt();
        await _userRepo.SaveChangesAsync();
    }

    // Generate Tokens
    string accessToken = TokenService.GenerateAccessToken(user);
    string refreshToken = TokenService.GenerateRefreshToken();


    return (accessToken, refreshToken);
}
    }

}


public class TokenService
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
