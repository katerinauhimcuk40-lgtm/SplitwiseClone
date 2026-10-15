namespace Splitwise.Domain.Entities;

public class Settlement
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int PayerId { get; set; }
    public User Payer { get; set; } = null!;

    public int PayeeId { get; set; }
    public User Payee { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}