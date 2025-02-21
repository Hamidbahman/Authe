using System.Threading.Tasks;
using Entities;

namespace auth.Repositories;

public interface IApplicationRepository
{
    Task<Application?> GetApplicationByClientIdAsync(string clientId);
    Task<bool> ValidateConfigurationLockAsync(long applicationId);
    Task<bool> ValidateConfigurationPasswordAsync(long applicationId, string password);
    Task<ConfigurationLock> GetConfigurationLockAsync(string clientId);
}