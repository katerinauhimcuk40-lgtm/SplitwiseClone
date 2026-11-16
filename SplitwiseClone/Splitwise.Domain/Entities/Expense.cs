namespace Splitwise.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int PaidById { get; set; }
    public User PaidBy { get; set; } = null!;

    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}