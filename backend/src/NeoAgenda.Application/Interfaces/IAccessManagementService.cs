using NeoAgenda.Application.DTOs;

namespace NeoAgenda.Application.Interfaces;

public interface IAccessManagementService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken ct = default);
    Task RevokeUserAccessAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<AccessLogDto>> GetAccessLogsAsync(int count = 100, CancellationToken ct = default);
    Task<IReadOnlyList<AccessLogDto>> GetUserAccessLogsAsync(string username, int count = 100, CancellationToken ct = default);
    Task RecordAccessLogAsync(string username, string action, string ipAddress, string userAgent, bool success, string? details = null, CancellationToken ct = default);
}
