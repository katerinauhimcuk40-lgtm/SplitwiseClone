using Microsoft.EntityFrameworkCore;
using Splitwise.Domain.Entities;

namespace Splitwise.Infrastructure.Persistence;

public class SplitwiseDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();
    public DbSet<Settlement> Settlements => Set<Settlement>();

    public SplitwiseDbContext(DbContextOptions<SplitwiseDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GroupMember>()
            .HasKey(gm => new { gm.UserId, gm.GroupId });

        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMembers)
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.Group)
            .WithMany(g => g.GroupMembers)
            .HasForeignKey(gm => gm.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.PaidBy)
            .WithMany(u => u.PaidExpenses)
            .HasForeignKey(e => e.PaidById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Expense>()
            .Property(e => e.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ExpenseSplit>()
            .Property(es => es.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Settlement>()
            .Property(s => s.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Settlement>()
            .HasOne(s => s.Payer)
            .WithMany()
            .HasForeignKey(s => s.PayerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Settlement>()
            .HasOne(s => s.Payee)
            .WithMany()
            .HasForeignKey(s => s.PayeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}