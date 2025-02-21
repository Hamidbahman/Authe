using System.Threading.Tasks;
using Entities;

namespace auth.Interfaces
{
    public interface IApplicationRepository
    {
        Task<Application?> GetApplicationByClientIdAsync(string clientId);
        Task<ConfigurationLock?> GetConfigurationLockAsync(string clientId);
    }
}
