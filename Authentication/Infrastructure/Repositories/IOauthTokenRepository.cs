using System;
using Entities;
namespace auth.Repositories;
    public interface IOauthTokenRepository
    {
        Task<OauthToken?> GetByAccessTokenAsync(string accessToken);
        Task<OauthToken?> GetByRefreshTokenAsync(string refreshToken);
        Task<OauthToken?> GetByClientAndUserNameAsync(string clientId, string userName);
        Task AddAsync(OauthToken token);
        Task UpdateAsync(OauthToken token);
        Task DeleteAsync(OauthToken token);
    }

