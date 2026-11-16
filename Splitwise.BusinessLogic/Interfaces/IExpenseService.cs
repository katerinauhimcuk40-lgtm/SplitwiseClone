using Splitwise.BusinessLogic.DTOs;

namespace Splitwise.BusinessLogic.Interfaces;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetGroupExpensesAsync(int groupId, int userId, CancellationToken ct = default);
    Task<ExpenseDto> GetByIdAsync(int expenseId, int userId, CancellationToken ct = default);
    Task<ExpenseDto> CreateAsync(int userId, CreateExpenseRequest request, CancellationToken ct = default);
    Task DeleteAsync(int expenseId, int userId, CancellationToken ct = default);
}
