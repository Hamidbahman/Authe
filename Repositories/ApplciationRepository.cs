using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Entities;
using Data;
using auth.Interfaces;

namespace auth.Repositories
{

    public class ApplicationRepository : IApplicationRepository
    {
        private readonly AutheDbContext _context;

        public ApplicationRepository(AutheDbContext context)
        {
            _context = context;
        }

        public async Task<Application?> GetApplicationByClientIdAsync(string clientId)
        {
                return await _context.Applications
                .FirstOrDefaultAsync(a => a.ClientId == clientId);
        }


        public async Task<ConfigurationLock?> GetConfigurationLockAsync(string clientId)
        {
            return await _context.ConfigurationLocks
                .Include(cl => cl.Application)
                .FirstOrDefaultAsync(cl => cl.Application.ClientId == clientId);
        }

    }
}
