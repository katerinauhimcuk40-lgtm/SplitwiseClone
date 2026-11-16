using Microsoft.EntityFrameworkCore;
using Splitwise.Domain.Entities;

namespace Splitwise.BusinessLogic.Abstractions;

/// <summary>
/// Абстракція доступу до даних для шару бізнес-логіки.
/// Реалізується в Infrastructure (SplitwiseDbContext).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Group> Groups { get; }
    DbSet<GroupMember> GroupMembers { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseSplit> ExpenseSplits { get; }
    DbSet<Settlement> Settlements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
