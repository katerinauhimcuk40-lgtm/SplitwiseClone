using Microsoft.EntityFrameworkCore;
using Splitwise.BusinessLogic.Abstractions;
using Splitwise.BusinessLogic.Exceptions;

namespace Splitwise.BusinessLogic.Services;

internal static class GroupAccessExtensions
{
    /// <summary>Перевіряє, що група існує і користувач є її учасником.</summary>
    public static async Task EnsureGroupMemberAsync(
        this IApplicationDbContext db, int groupId, int userId, CancellationToken ct)
    {
        var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId, ct);
        if (!groupExists)
            throw new GroupNotFoundException(groupId);

        var isMember = await db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId, ct);
        if (!isMember)
            throw new NotGroupMemberException();
    }
}
