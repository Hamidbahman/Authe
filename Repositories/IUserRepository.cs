using System;
using Entities;

namespace auth.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ValidatePasswordAsync(string username, string password);
    Task<bool> CheckLoginPolicyAsync(string username);
    Task<LoginPolicy> GetLoginPoliciesByUserID(long userId);
}