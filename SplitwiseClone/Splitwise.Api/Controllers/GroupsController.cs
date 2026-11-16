using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Api.Extensions;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Interfaces;

namespace Splitwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groups;

    public GroupsController(IGroupService groups)
    {
        _groups = groups;
    }

    /// <summary>Групи, у яких бере участь поточний користувач.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> GetGroups(CancellationToken ct)
        => Ok(await _groups.GetUserGroupsAsync(User.GetUserId(), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GroupDto>> GetGroup(int id, CancellationToken ct)
        => Ok(await _groups.GetByIdAsync(id, User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<GroupDto>> CreateGroup(CreateGroupRequest request, CancellationToken ct)
    {
        var group = await _groups.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
    }

    /// <summary>Додати учасника до групи за email.</summary>
    [HttpPost("{id:int}/members")]
    public async Task<ActionResult<GroupDto>> AddMember(int id, AddGroupMemberRequest request, CancellationToken ct)
        => Ok(await _groups.AddMemberAsync(id, User.GetUserId(), request, ct));
}
