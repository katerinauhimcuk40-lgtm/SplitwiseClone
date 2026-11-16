using Splitwise.BusinessLogic.DTOs;

namespace Splitwise.BusinessLogic.Interfaces;

public interface IGroupService
{
    Task<IReadOnlyList<GroupDto>> GetUserGroupsAsync(int userId, CancellationToken ct = default);
    Task<GroupDto> GetByIdAsync(int groupId, int userId, CancellationToken ct = default);
    Task<GroupDto> CreateAsync(int userId, CreateGroupRequest request, CancellationToken ct = default);
    Task<GroupDto> AddMemberAsync(int groupId, int userId, AddGroupMemberRequest request, CancellationToken ct = default);
}
