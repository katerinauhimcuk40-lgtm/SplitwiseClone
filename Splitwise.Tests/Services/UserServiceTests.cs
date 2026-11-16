using Splitwise.BusinessLogic.Exceptions;
using Splitwise.BusinessLogic.Services;
using Splitwise.Tests.Helpers;

namespace Splitwise.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_ReturnsProfile()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var service = new UserService(db);

        var result = await service.GetCurrentAsync(alice.Id);

        Assert.Equal(alice.Id, result.Id);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("alice@example.com", result.UserName);
    }

    [Fact]
    public async Task GetCurrentAsync_UnknownUser_Throws()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var service = new UserService(db);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetCurrentAsync(999));
    }

    [Fact]
    public async Task GetContactsAsync_ReturnsOnlyUsersSharingAGroup()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var bob = await TestDb.AddUserAsync(db, "bob@example.com");
        var carol = await TestDb.AddUserAsync(db, "carol@example.com");
        var dave = await TestDb.AddUserAsync(db, "dave@example.com");
        await TestDb.AddGroupAsync(db, "Alice+Bob", alice, bob);
        await TestDb.AddGroupAsync(db, "Carol+Dave", carol, dave);
        var service = new UserService(db);

        var contacts = await service.GetContactsAsync(alice.Id);

        var contact = Assert.Single(contacts);
        Assert.Equal(bob.Id, contact.Id);
    }

    [Fact]
    public async Task GetContactsAsync_WithoutGroups_ReturnsEmpty()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        await TestDb.AddUserAsync(db, "bob@example.com");
        var service = new UserService(db);

        var contacts = await service.GetContactsAsync(alice.Id);

        Assert.Empty(contacts);
    }
}
