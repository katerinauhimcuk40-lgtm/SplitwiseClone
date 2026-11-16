using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Api.Extensions;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Interfaces;

namespace Splitwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    /// <summary>Профіль поточного користувача.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
        => Ok(await _users.GetCurrentAsync(User.GetUserId(), ct));

    /// <summary>Користувачі, з якими є спільні групи.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetContacts(CancellationToken ct)
        => Ok(await _users.GetContactsAsync(User.GetUserId(), ct));
}
