using System.Threading.Tasks;
using Authentication.Domain.Entities;

namespace Authentication.Domain.Repositories
{
    public interface IApplicationRepository
    {
        Task<Application?> GetApplicationByClientIdAsync(string clientId);
        Task<ConfigurationLock?> GetConfigurationLockAsync(string clientId);
    }
}
