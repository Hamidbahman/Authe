using Data;
using Microsoft.EntityFrameworkCore;

namespace Queries;
public class GetUserPasswordQuery
{
    private readonly AutheDbContext _context;

    public GetUserPasswordQuery(AutheDbContext context)
    {
        _context = context;
    }

    public async Task<string?> ExecuteAsync(long userId)
    {
        var userPassword = await _context.Users
            .Include(u => u.UserProperty)
            .Where(u => u.Id == userId)
            .Select(u => u.UserProperty.Password)
            .FirstOrDefaultAsync();

        return userPassword;
    }
}