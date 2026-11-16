using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Exceptions;
using Splitwise.BusinessLogic.Services;
using Splitwise.Tests.Helpers;

namespace Splitwise.Tests.Services;

public class GroupServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsName_AndAddsCreatorAsMember()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var service = new GroupService(db);

        var result = await service.CreateAsync(alice.Id, new CreateGroupRequest("  Trip  ", " Weekend "));

        Assert.Equal("Trip", result.Name);
        Assert.Equal("Weekend", result.Description);
        var member = Assert.Single(result.Members);
        Assert.Equal(alice.Id, member.Id);
    }

    [Fact]
    public async Task CreateAsync_NullDescription_BecomesEmptyString()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var service = new GroupService(db);

        var result = await service.CreateAsync(alice.Id, new CreateGroupRequest("Trip", null));

        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public async Task GetUserGroupsAsync_ReturnsOnlyGroupsOfUser_OrderedByName()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var bob = await TestDb.AddUserAsync(db, "bob@example.com");
        await TestDb.AddGroupAsync(db, "Zebra", alice);
        await TestDb.AddGroupAsync(db, "Alpha", alice, bob);
        await TestDb.AddGroupAsync(db, "Bobs only", bob);
        var service = new GroupService(db);

        var groups = await service.GetUserGroupsAsync(alice.Id);

        Assert.Equal(new[] { "Alpha", "Zebra" }, groups.Select(g => g.Name));
    }

    [Fact]
    public async Task GetByIdAsync_Member_ReturnsGroupWithMembers()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var bob = await TestDb.AddUserAsync(db, "bob@example.com");
        var group = await TestDb.AddGroupAsync(db, "Trip", alice, bob);
        var service = new GroupService(db);

        var result = await service.GetByIdAsync(group.Id, alice.Id);

        Assert.Equal(group.Id, result.Id);
        Assert.Equal(2, result.Members.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NotMember_ThrowsForbidden()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var outsider = await TestDb.AddUserAsync(db, "eve@example.com");
        var group = await TestDb.AddGroupAsync(db, "Trip", alice);
        var service = new GroupService(db);

        await Assert.ThrowsAsync<NotGroupMemberException>(() => service.GetByIdAsync(group.Id, outsider.Id));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownGroup_ThrowsNotFound()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var service = new GroupService(db);

        await Assert.ThrowsAsync<GroupNotFoundException>(() => service.GetByIdAsync(999, alice.Id));
    }

    [Fact]
    public async Task AddMemberAsync_AddsUserByEmail_CaseInsensitive()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var bob = await TestDb.AddUserAsync(db, "bob@example.com");
        var group = await TestDb.AddGroupAsync(db, "Trip", alice);
        var service = new GroupService(db);

        var result = await service.AddMemberAsync(group.Id, alice.Id, new AddGroupMemberRequest("  BOB@Example.com "));

        Assert.Equal(2, result.Members.Count);
        Assert.Contains(result.Members, m => m.Id == bob.Id);
    }

    [Fact]
    public async Task AddMemberAsync_UnknownEmail_ThrowsUserNotFound()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var group = await TestDb.AddGroupAsync(db, "Trip", alice);
        var service = new GroupService(db);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.AddMemberAsync(group.Id, alice.Id, new AddGroupMemberRequest("nobody@example.com")));
    }

    [Fact]
    public async Task AddMemberAsync_AlreadyMember_ThrowsConflict()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var bob = await TestDb.AddUserAsync(db, "bob@example.com");
        var group = await TestDb.AddGroupAsync(db, "Trip", alice, bob);
        var service = new GroupService(db);

        await Assert.ThrowsAsync<UserAlreadyInGroupException>(() =>
            service.AddMemberAsync(group.Id, alice.Id, new AddGroupMemberRequest("bob@example.com")));
    }

    [Fact]
    public async Task AddMemberAsync_RequesterNotMember_ThrowsForbidden()
    {
        var testDb = new TestDb();
        await using var db = testDb.NewContext();
        var alice = await TestDb.AddUserAsync(db, "alice@example.com");
        var eve = await TestDb.AddUserAsync(db, "eve@example.com");
        await TestDb.AddUserAsync(db, "bob@example.com");
        var group = await TestDb.AddGroupAsync(db, "Trip", alice);
        var service = new GroupService(db);

        await Assert.ThrowsAsync<NotGroupMemberException>(() =>
            service.AddMemberAsync(group.Id, eve.Id, new AddGroupMemberRequest("bob@example.com")));
    }
}
