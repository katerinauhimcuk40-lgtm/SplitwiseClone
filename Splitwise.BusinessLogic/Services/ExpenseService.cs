using Microsoft.EntityFrameworkCore;
using Splitwise.BusinessLogic.Abstractions;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Exceptions;
using Splitwise.BusinessLogic.Interfaces;
using Splitwise.BusinessLogic.Mapping;
using Splitwise.Domain.Entities;

namespace Splitwise.BusinessLogic.Services;

public class ExpenseService : IExpenseService
{
    private readonly IApplicationDbContext _db;

    public ExpenseService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetGroupExpensesAsync(
        int groupId, int userId, CancellationToken ct = default)
    {
        await _db.EnsureGroupMemberAsync(groupId, userId, ct);

        var expenses = await QueryWithDetails()
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);

        return expenses.Select(e => e.ToDto()).ToList();
    }

    public async Task<ExpenseDto> GetByIdAsync(int expenseId, int userId, CancellationToken ct = default)
    {
        var expense = await QueryWithDetails().FirstOrDefaultAsync(e => e.Id == expenseId, ct)
                      ?? throw new NotFoundException($"Витрату з id={expenseId} не знайдено.");

        await _db.EnsureGroupMemberAsync(expense.GroupId, userId, ct);
        return expense.ToDto();
    }

    public async Task<ExpenseDto> CreateAsync(int userId, CreateExpenseRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new BusinessRuleException("Опис витрати обов'язковий.");
        if (request.Amount <= 0)
            throw new BusinessRuleException("Сума витрати має бути більшою за нуль.");
        if (decimal.Round(request.Amount, 2) != request.Amount)
            throw new BusinessRuleException("Сума витрати може мати не більше двох знаків після коми.");

        await _db.EnsureGroupMemberAsync(request.GroupId, userId, ct);

        var memberIds = await _db.GroupMembers
            .Where(gm => gm.GroupId == request.GroupId)
            .Select(gm => gm.UserId)
            .OrderBy(id => id)
            .ToListAsync(ct);

        var payerId = request.PaidById ?? userId;
        if (!memberIds.Contains(payerId))
            throw new BusinessRuleException("Платник має бути учасником групи.");

        var shares = request.Splits is { Count: > 0 }
            ? ValidateCustomSplits(request.Splits, memberIds, request.Amount)
            : CalculateEqualSplits(memberIds, request.Amount);

        var expense = new Expense
        {
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Date = request.Date ?? DateTime.UtcNow,
            GroupId = request.GroupId,
            PaidById = payerId
        };

        foreach (var (splitUserId, splitAmount) in shares)
        {
            expense.Splits.Add(new ExpenseSplit
            {
                UserId = splitUserId,
                Amount = splitAmount,
                // Власна частка платника вважається вже сплаченою.
                IsPaid = splitUserId == payerId
            });
        }

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        var created = await QueryWithDetails().FirstAsync(e => e.Id == expense.Id, ct);
        return created.ToDto();
    }

    public async Task DeleteAsync(int expenseId, int userId, CancellationToken ct = default)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId, ct)
                      ?? throw new NotFoundException($"Витрату з id={expenseId} не знайдено.");

        await _db.EnsureGroupMemberAsync(expense.GroupId, userId, ct);

        if (expense.PaidById != userId)
            throw new ForbiddenException("Видалити витрату може лише той, хто її оплатив.");

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync(ct);
    }

    private IQueryable<Expense> QueryWithDetails() =>
        _db.Expenses
            .AsNoTracking()
            .Include(e => e.PaidBy)
            .Include(e => e.Splits)
            .ThenInclude(s => s.User);

    /// <summary>Рівний поділ у копійках; залишок розподіляється по одній копійці перших учасників.</summary>
    private static List<(int UserId, decimal Amount)> CalculateEqualSplits(IReadOnlyList<int> memberIds, decimal amount)
    {
        var totalCents = (long)(amount * 100m);
        var baseShare = totalCents / memberIds.Count;
        var remainder = (int)(totalCents % memberIds.Count);

        var result = new List<(int, decimal)>(memberIds.Count);
        for (var i = 0; i < memberIds.Count; i++)
        {
            var cents = baseShare + (i < remainder ? 1 : 0);
            result.Add((memberIds[i], cents / 100m));
        }

        return result;
    }

    private static List<(int UserId, decimal Amount)> ValidateCustomSplits(
        List<ExpenseSplitRequest> splits, IReadOnlyList<int> memberIds, decimal amount)
    {
        if (splits.Any(s => s.Amount <= 0 || decimal.Round(s.Amount, 2) != s.Amount))
            throw new BusinessRuleException("Кожна частка має бути додатною сумою з не більше ніж двома знаками після коми.");

        if (splits.GroupBy(s => s.UserId).Any(g => g.Count() > 1))
            throw new BusinessRuleException("Користувач не може бути вказаний у розподілі більше одного разу.");

        if (splits.Any(s => !memberIds.Contains(s.UserId)))
            throw new BusinessRuleException("Усі учасники розподілу мають бути учасниками групи.");

        if (splits.Sum(s => s.Amount) != amount)
            throw new BusinessRuleException("Сума часток має дорівнювати сумі витрати.");

        return splits.Select(s => (s.UserId, s.Amount)).ToList();
    }
}
