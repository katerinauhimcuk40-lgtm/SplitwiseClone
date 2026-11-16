using Microsoft.EntityFrameworkCore;
using Splitwise.Domain.Entities;
using Splitwise.Infrastructure.Persistence;

namespace Splitwise.Tests.Helpers;

/// <summary>
/// Ізольована in-memory база для одного тесту. Кожен виклик NewContext() повертає
/// новий контекст до тієї самої бази — зручно перевіряти стан "з нуля", без кешу трекера.
/// </summary>
public sealed class TestDb
{
    private readonly DbContextOptions<SplitwiseDbContext> _options =
        new DbContextOptionsBuilder<SplitwiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    public SplitwiseDbContext NewContext() => new(_options);

    public static async Task<User> AddUserAsync(SplitwiseDbContext db, string email)
    {
        var user = new User
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<Group> AddGroupAsync(SplitwiseDbContext db, string name, params User[] members)
    {
        var group = new Group { Name = name, Description = string.Empty };
        foreach (var member in members)
            group.GroupMembers.Add(new GroupMember { UserId = member.Id });

        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }
}
