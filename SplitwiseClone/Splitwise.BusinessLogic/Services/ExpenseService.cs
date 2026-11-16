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
                      ?? throw new ExpenseNotFoundException(expenseId);

        await _db.EnsureGroupMemberAsync(expense.GroupId, userId, ct);
        return expense.ToDto();
    }

    public async Task<ExpenseDto> CreateAsync(int userId, CreateExpenseRequest request, CancellationToken ct = default)
    {
        // Формат DTO (сума, описи, частки тощо) перевіряє CreateExpenseRequestValidator.
        // Тут — лише правила, що потребують даних із БД.
        await _db.EnsureGroupMemberAsync(request.GroupId, userId, ct);

        var memberIds = await _db.GroupMembers
            .Where(gm => gm.GroupId == request.GroupId)
            .Select(gm => gm.UserId)
            .OrderBy(id => id)
            .ToListAsync(ct);

        var payerId = request.PaidById ?? userId;
        if (!memberIds.Contains(payerId))
            throw new InvalidExpenseException("Платник має бути учасником групи.");

        var shares = request.Splits is { Count: > 0 }
            ? ValidateCustomSplits(request.Splits, memberIds)
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
                      ?? throw new ExpenseNotFoundException(expenseId);

        await _db.EnsureGroupMemberAsync(expense.GroupId, userId, ct);

        if (expense.PaidById != userId)
            throw new ExpenseDeletionNotAllowedException();

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

    /// <summary>Перевірка, що потребує даних групи: усі учасники розподілу мають бути її учасниками.</summary>
    private static List<(int UserId, decimal Amount)> ValidateCustomSplits(
        List<ExpenseSplitRequest> splits, IReadOnlyList<int> memberIds)
    {
        if (splits.Any(s => !memberIds.Contains(s.UserId)))
            throw new InvalidExpenseException("Усі учасники розподілу мають бути учасниками групи.");

        return splits.Select(s => (s.UserId, s.Amount)).ToList();
    }
}
