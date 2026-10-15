namespace Splitwise.Domain.Entities;

public class ExpenseSplit
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public Expense Expense { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Amount { get; set; }
    public bool IsPaid { get; set; } = false;
}