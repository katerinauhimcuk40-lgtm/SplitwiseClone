using Microsoft.EntityFrameworkCore;
using Splitwise.BusinessLogic.Abstractions;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Exceptions;
using Splitwise.BusinessLogic.Interfaces;
using Splitwise.BusinessLogic.Mapping;
using Splitwise.Domain.Entities;

namespace Splitwise.BusinessLogic.Services;

public class GroupService : IGroupService
{
    private const int MaxNameLength = 100;

    private readonly IApplicationDbContext _db;

    public GroupService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GroupDto>> GetUserGroupsAsync(int userId, CancellationToken ct = default)
    {
        var groups = await _db.Groups
            .AsNoTracking()
            .Include(g => g.GroupMembers)
            .ThenInclude(gm => gm.User)
            .Where(g => g.GroupMembers.Any(gm => gm.UserId == userId))
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        return groups.Select(g => g.ToDto()).ToList();
    }

    public async Task<GroupDto> GetByIdAsync(int groupId, int userId, CancellationToken ct = default)
    {
        await _db.EnsureGroupMemberAsync(groupId, userId, ct);
        return await LoadGroupDtoAsync(groupId, ct);
    }

    public async Task<GroupDto> CreateAsync(int userId, CreateGroupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleException("Назва групи обов'язкова.");

        var name = request.Name.Trim();
        if (name.Length > MaxNameLength)
            throw new BusinessRuleException($"Назва групи не може бути довшою за {MaxNameLength} символів.");

        var group = new Group
        {
            Name = name,
            Description = request.Description?.Trim() ?? string.Empty
        };

        // Автор групи автоматично стає її першим учасником.
        group.GroupMembers.Add(new GroupMember { UserId = userId });

        _db.Groups.Add(group);
        await _db.SaveChangesAsync(ct);

        return await LoadGroupDtoAsync(group.Id, ct);
    }

    public async Task<GroupDto> AddMemberAsync(
        int groupId, int userId, AddGroupMemberRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new BusinessRuleException("Email користувача обов'язковий.");

        await _db.EnsureGroupMemberAsync(groupId, userId, ct);

        // Identity за замовчуванням нормалізує email через ToUpperInvariant().
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var newMember = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct)
                        ?? throw new NotFoundException("Користувача з таким email не знайдено.");

        var alreadyMember = await _db.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == newMember.Id, ct);
        if (alreadyMember)
            throw new ConflictException("Користувач уже є учасником групи.");

        _db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = newMember.Id });
        await _db.SaveChangesAsync(ct);

        return await LoadGroupDtoAsync(groupId, ct);
    }

    private async Task<GroupDto> LoadGroupDtoAsync(int groupId, CancellationToken ct)
    {
        var group = await _db.Groups
            .AsNoTracking()
            .Include(g => g.GroupMembers)
            .ThenInclude(gm => gm.User)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct)
            ?? throw new NotFoundException($"Групу з id={groupId} не знайдено.");

        return group.ToDto();
    }
}
