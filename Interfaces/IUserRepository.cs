using System.Threading.Tasks;
using Entities;
using Enums;

namespace auth.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> ValidatePasswordAsync(string username, string password);
        Task<bool> CheckLoginPolicyAsync(string username);
        Task<LoginPolicy?> GetLoginPoliciesByUserID(string userId);
        Task<(string Username, string Password)?> GetUserCredentialsAsync(string username);
    }
}
