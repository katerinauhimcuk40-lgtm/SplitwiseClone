namespace Splitwise.BusinessLogic.DTOs;

/// <param name="PaidById">Хто заплатив. Якщо не вказано — поточний користувач.</param>
/// <param name="Splits">Частки учасників. Якщо порожньо — витрата ділиться порівну між усіма учасниками групи.</param>
public record CreateExpenseRequest(
    int GroupId,
    string Description,
    decimal Amount,
    DateTime? Date,
    int? PaidById,
    List<ExpenseSplitRequest>? Splits);

public record ExpenseSplitRequest(int UserId, decimal Amount);

public record ExpenseSplitDto(int UserId, string UserName, decimal Amount, bool IsPaid);

public record ExpenseDto(
    int Id,
    string Description,
    decimal Amount,
    DateTime Date,
    int GroupId,
    UserDto PaidBy,
    IReadOnlyList<ExpenseSplitDto> Splits);
