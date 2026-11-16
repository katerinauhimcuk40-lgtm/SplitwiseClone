namespace Splitwise.BusinessLogic.DTOs;

public record CreateGroupRequest(string Name, string? Description);

public record AddGroupMemberRequest(string Email);

public record GroupDto(int Id, string Name, string Description, IReadOnlyList<UserDto> Members);
