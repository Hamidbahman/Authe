using System;


using System.Threading.Tasks;
using Entities;
using Data; // Assuming your DbContext is in the Data namespace
using Microsoft.EntityFrameworkCore;

namespace auth.Repositories
{
    public class OauthTokenRepository : IOauthTokenRepository
    {
        private readonly AutheDbContext _context;

        public OauthTokenRepository(AutheDbContext context)
        {
            _context = context;
        }

        public async Task<OauthToken?> GetByAccessTokenAsync(string accessToken)
        {
            return await _context.OauthTokens
                .FirstOrDefaultAsync(t => t.AccessToken == accessToken);
        }

        public async Task<OauthToken?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.OauthTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
        }

        public async Task<OauthToken?> GetByClientAndUserNameAsync(string clientId, string userName)
        {
            return await _context.OauthTokens
                .FirstOrDefaultAsync(t => t.ClientId == clientId && t.UserName == userName);
        }

        public async Task AddAsync(OauthToken token)
        {
            await _context.OauthTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(OauthToken token)
        {
            _context.OauthTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(OauthToken token)
        {
            _context.OauthTokens.Remove(token);
            await _context.SaveChangesAsync();
        }
    }
}
