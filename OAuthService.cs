using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using auth.Repositories;

namespace Auth
{
    public class OAuthService 
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepo;
        private readonly IOauthTokenRepository _OauthRepo;
        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(IApplicationRepository applicationRepository, IUserRepository userRepository,
            IOauthTokenRepository oauthTokenRepository)
        {
            _OauthRepo = oauthTokenRepository;
            _applicationRepository = applicationRepository;
            _userRepo = userRepository;
        }

public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret)
{
    var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
    if (application == null || clientSecret != application.ClientScope)
        return null;

    var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
    if (configLock != null && configLock.CaptchaNeeded)
        return null;

    string authCode = Guid.NewGuid().ToString();

    _authCodes[clientId] = authCode;

    return authCode;
}

public async Task<(string accessToken, string refreshToken)?> ValidateAndGenerateTokensAsync(
    string clientId, string username, string password, string authenticationCode)
{
    if (!_authCodes.TryGetValue(clientId, out var storedAuthCode) || storedAuthCode != authenticationCode)
        return null;

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return null; 

                
            if(username != user.Username || password != user.UserProperty.Password)
                return null;
            

            var loginPolicy = await _userRepo.GetLoginPoliciesByUserID(user.Id);
            if(loginPolicy.LockTypes == Enums.LockTypes.None)
            {
                string accessToken = Guid.NewGuid().ToString();
                string refreshToken = Guid.NewGuid().ToString();
                _authCodes.TryRemove(clientId, out _);
                return (accessToken, refreshToken);
            }
            else
            {
                return null;
            }





}

    }
}
