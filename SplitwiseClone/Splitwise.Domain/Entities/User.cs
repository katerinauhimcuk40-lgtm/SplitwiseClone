using Microsoft.AspNetCore.Identity;

namespace Splitwise.Domain.Entities;

/// <summary>
/// Користувач системи. Успадковує IdentityUser, тому Id, UserName, Email,
/// PasswordHash тощо надає ASP.NET Core Identity.
/// </summary>
public class User : IdentityUser<int>
{
    public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();
}
