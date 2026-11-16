using Splitwise.BusinessLogic.DTOs;

namespace Splitwise.BusinessLogic.Interfaces;

public interface IUserService
{
    Task<UserDto> GetCurrentAsync(int userId, CancellationToken ct = default);

    /// <summary>Користувачі, з якими поточний користувач має хоча б одну спільну групу.</summary>
    Task<IReadOnlyList<UserDto>> GetContactsAsync(int userId, CancellationToken ct = default);
}
