using Microsoft.EntityFrameworkCore;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Exceptions;
using Splitwise.BusinessLogic.Services;
using Splitwise.Domain.Entities;
using Splitwise.Infrastructure.Persistence;
using Splitwise.Tests.Helpers;

namespace Splitwise.Tests.Services;

public class ExpenseServiceTests
{
    private sealed record Scenario(
        TestDb TestDb, SplitwiseDbContext Db, ExpenseService Service,
        User Alice, User Bob, User Carol, User Outsider, Group Group);

    /// <summary>Група "Trip" з Alice, Bob, Carol та стороннім користувачем Outsider.</summary>
    private static async Task<Scenario> CreateScenarioAsync()
    {
        var testDb = new TestDb();
        var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var bob = await TestDb.AddUserAsync(db, "bob@example.com");
        var carol = await TestDb.AddUserAsync(db, "carol@example.com");
        var outsider = await TestDb.AddUserAsync(db, "eve@example.com");
        var group = await TestDb.AddGroupAsync(db, "Trip", alice, bob, carol);
        return new Scenario(testDb, db, new ExpenseService(db), alice, bob, carol, outsider, group);
    }

    private static CreateExpenseRequest Request(
        int groupId, decimal amount = 100m, string description = "Dinner",
        DateTime? date = null, int? paidById = null, List<ExpenseSplitRequest>? splits = null)
        => new(groupId, description, amount, date, paidById, splits);

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_WithoutSplits_DividesEquallyAndDistributesRemainderCents()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        var result = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 100m));

        Assert.Equal(3, result.Splits.Count);
        Assert.Equal(100m, result.Splits.Sum(x => x.Amount));
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, result.Splits.Select(x => x.Amount).OrderBy(a => a));
    }

    [Fact]
    public async Task CreateAsync_DefaultsPayerToCurrentUser_AndMarksOwnShareAsPaid()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        var result = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 90m));

        Assert.Equal(s.Alice.Id, result.PaidBy.Id);
        Assert.True(result.Splits.Single(x => x.UserId == s.Alice.Id).IsPaid);
        Assert.All(result.Splits.Where(x => x.UserId != s.Alice.Id), x => Assert.False(x.IsPaid));
    }

    [Fact]
    public async Task CreateAsync_ExplicitPayer_IsUsed()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        var result = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 90m, paidById: s.Bob.Id));

        Assert.Equal(s.Bob.Id, result.PaidBy.Id);
        Assert.True(result.Splits.Single(x => x.UserId == s.Bob.Id).IsPaid);
    }

    [Fact]
    public async Task CreateAsync_WithCustomSplits_StoresThemAsGiven()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var splits = new List<ExpenseSplitRequest> { new(s.Alice.Id, 70m), new(s.Bob.Id, 30m) };

        var result = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 100m, splits: splits));

        Assert.Equal(2, result.Splits.Count);
        Assert.Equal(70m, result.Splits.Single(x => x.UserId == s.Alice.Id).Amount);
        Assert.Equal(30m, result.Splits.Single(x => x.UserId == s.Bob.Id).Amount);
    }

    [Fact]
    public async Task CreateAsync_TrimsDescription_AndPersistsExpense()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        var result = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 10m, "  Taxi  "));

        Assert.Equal("Taxi", result.Description);
        await using var verify = s.TestDb.NewContext();
        Assert.Equal(1, await verify.Expenses.CountAsync());
        Assert.Equal(3, await verify.ExpenseSplits.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_UsesProvidedDate()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var date = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 10m, date: date));

        Assert.Equal(date, result.Date);
    }

    [Fact]
    public async Task CreateAsync_RequesterNotMember_ThrowsForbidden()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        await Assert.ThrowsAsync<NotGroupMemberException>(() =>
            s.Service.CreateAsync(s.Outsider.Id, Request(s.Group.Id)));
    }

    [Fact]
    public async Task CreateAsync_UnknownGroup_ThrowsNotFound()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        await Assert.ThrowsAsync<GroupNotFoundException>(() =>
            s.Service.CreateAsync(s.Alice.Id, Request(999)));
    }

    [Fact]
    public async Task CreateAsync_PayerNotInGroup_ThrowsInvalidExpense()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        await Assert.ThrowsAsync<InvalidExpenseException>(() =>
            s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, paidById: s.Outsider.Id)));
    }

    [Fact]
    public async Task CreateAsync_SplitUserNotInGroup_ThrowsInvalidExpense()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var splits = new List<ExpenseSplitRequest> { new(s.Alice.Id, 50m), new(s.Outsider.Id, 50m) };

        await Assert.ThrowsAsync<InvalidExpenseException>(() =>
            s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 100m, splits: splits)));
    }

    // ---------- Get ----------

    [Fact]
    public async Task GetGroupExpensesAsync_ReturnsNewestFirst()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 10m, "Old", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 20m, "New", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 30m, "Mid", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await s.Service.GetGroupExpensesAsync(s.Group.Id, s.Bob.Id);

        Assert.Equal(new[] { "New", "Mid", "Old" }, result.Select(e => e.Description));
    }

    [Fact]
    public async Task GetGroupExpensesAsync_ReturnsOnlyExpensesOfRequestedGroup()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var otherGroup = await TestDb.AddGroupAsync(s.Db, "Other", s.Alice);
        await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 10m, "Trip expense"));
        await s.Service.CreateAsync(s.Alice.Id, Request(otherGroup.Id, 20m, "Other expense"));

        var result = await s.Service.GetGroupExpensesAsync(s.Group.Id, s.Alice.Id);

        Assert.Equal("Trip expense", Assert.Single(result).Description);
    }

    [Fact]
    public async Task GetGroupExpensesAsync_NotMember_ThrowsForbidden()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        await Assert.ThrowsAsync<NotGroupMemberException>(() =>
            s.Service.GetGroupExpensesAsync(s.Group.Id, s.Outsider.Id));
    }

    [Fact]
    public async Task GetByIdAsync_Member_ReturnsExpenseWithSplits()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var created = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id, 90m));

        var result = await s.Service.GetByIdAsync(created.Id, s.Carol.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(3, result.Splits.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NotMember_ThrowsForbidden()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var created = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id));

        await Assert.ThrowsAsync<NotGroupMemberException>(() =>
            s.Service.GetByIdAsync(created.Id, s.Outsider.Id));
    }

    [Fact]
    public async Task GetByIdAsync_Unknown_ThrowsNotFound()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        await Assert.ThrowsAsync<ExpenseNotFoundException>(() => s.Service.GetByIdAsync(999, s.Alice.Id));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAsync_ByPayer_RemovesExpense()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var created = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id));

        await s.Service.DeleteAsync(created.Id, s.Alice.Id);

        await using var verify = s.TestDb.NewContext();
        Assert.False(await verify.Expenses.AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_ByOtherMember_ThrowsForbidden_AndKeepsExpense()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var created = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id));

        await Assert.ThrowsAsync<ExpenseDeletionNotAllowedException>(() =>
            s.Service.DeleteAsync(created.Id, s.Bob.Id));

        await using var verify = s.TestDb.NewContext();
        Assert.True(await verify.Expenses.AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_ByNonMember_ThrowsForbidden()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;
        var created = await s.Service.CreateAsync(s.Alice.Id, Request(s.Group.Id));

        await Assert.ThrowsAsync<NotGroupMemberException>(() =>
            s.Service.DeleteAsync(created.Id, s.Outsider.Id));
    }

    [Fact]
    public async Task DeleteAsync_Unknown_ThrowsNotFound()
    {
        var s = await CreateScenarioAsync();
        await using var _ = s.Db;

        await Assert.ThrowsAsync<ExpenseNotFoundException>(() => s.Service.DeleteAsync(999, s.Alice.Id));
    }
}
