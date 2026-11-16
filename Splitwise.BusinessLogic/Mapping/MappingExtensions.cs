using Splitwise.BusinessLogic.DTOs;
using Splitwise.Domain.Entities;

namespace Splitwise.BusinessLogic.Mapping;

internal static class MappingExtensions
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty);

    public static GroupDto ToDto(this Group group) =>
        new(
            group.Id,
            group.Name,
            group.Description,
            group.GroupMembers
                .Select(gm => gm.User.ToDto())
                .OrderBy(u => u.UserName)
                .ToList());

    public static ExpenseDto ToDto(this Expense expense) =>
        new(
            expense.Id,
            expense.Description,
            expense.Amount,
            expense.Date,
            expense.GroupId,
            expense.PaidBy.ToDto(),
            expense.Splits
                .Select(s => new ExpenseSplitDto(s.UserId, s.User.UserName ?? string.Empty, s.Amount, s.IsPaid))
                .ToList());
}
