using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Splitwise.Domain.Entities;
using Splitwise.Infrastructure.Persistence;

namespace Splitwise.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly SplitwiseDbContext _context;

    public GroupsController(SplitwiseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Group>>> GetGroups()
    {
        return await _context.Groups
            .Include(g => g.GroupMembers)
            .ThenInclude(gm => gm.User)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Group>> CreateGroup(Group group)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGroups), new { id = group.Id }, group);
    }
}