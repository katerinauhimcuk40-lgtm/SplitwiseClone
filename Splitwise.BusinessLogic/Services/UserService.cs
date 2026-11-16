using Microsoft.EntityFrameworkCore;
using Splitwise.BusinessLogic.Abstractions;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Exceptions;
using Splitwise.BusinessLogic.Interfaces;
using Splitwise.BusinessLogic.Mapping;

namespace Splitwise.BusinessLogic.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;

    public UserService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserDto> GetCurrentAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("Користувача не знайдено.");
        return user.ToDto();
    }

    public async Task<IReadOnlyList<UserDto>> GetContactsAsync(int userId, CancellationToken ct = default)
    {
        var myGroupIds = _db.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Select(gm => gm.GroupId);

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id != userId && u.GroupMembers.Any(gm => myGroupIds.Contains(gm.GroupId)))
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

        return users.Select(u => u.ToDto()).ToList();
    }
}
