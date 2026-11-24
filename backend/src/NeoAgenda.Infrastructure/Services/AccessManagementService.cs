using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;
using NeoAgenda.Domain.Common;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Infrastructure.Security;

namespace NeoAgenda.Infrastructure.Services;

public class AccessManagementService : IAccessManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccessLogRepository _accessLogRepository;

    public AccessManagementService(IUserRepository userRepository, IAccessLogRepository accessLogRepository)
    {
        _userRepository = userRepository;
        _accessLogRepository = accessLogRepository;
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        return users.Select(u => new UserDto(
            u.Id,
            u.Username,
            u.Name,
            u.Role,
            u.CreatedAt,
            HasActiveSession: u.RefreshToken != null && u.RefreshTokenExpiresAt > DateTime.UtcNow
        )).ToList();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            throw new DomainException("O nome de usuário deve ter pelo menos 3 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new DomainException("A senha deve ter pelo menos 6 caracteres.");

        var existing = await _userRepository.GetByUsernameAsync(request.Username, ct);
        if (existing != null)
            throw new DomainException($"Já existe um usuário cadastrado com o login '{request.Username}'.");

        var passwordHash = PasswordHasher.HashPassword(request.Password);
        var user = new User(request.Username, passwordHash, request.Name, string.IsNullOrWhiteSpace(request.Role) ? "Admin" : request.Role);

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        return new UserDto(
            user.Id,
            user.Username,
            user.Name,
            user.Role,
            user.CreatedAt,
            HasActiveSession: false
        );
    }

    public async Task RevokeUserAccessAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            throw new DomainException("Usuário não encontrado.");

        user.RevokeRefreshToken();
        await _userRepository.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AccessLogDto>> GetAccessLogsAsync(int count = 100, CancellationToken ct = default)
    {
        var logs = await _accessLogRepository.GetRecentAsync(count, ct);
        return logs.Select(l => new AccessLogDto(
            l.Id,
            l.Username,
            l.Action,
            l.IpAddress,
            l.UserAgent,
            l.Success,
            l.Details,
            l.Timestamp
        )).ToList();
    }

    public async Task<IReadOnlyList<AccessLogDto>> GetUserAccessLogsAsync(string username, int count = 100, CancellationToken ct = default)
    {
        var logs = await _accessLogRepository.GetByUserAsync(username, count, ct);
        return logs.Select(l => new AccessLogDto(
            l.Id,
            l.Username,
            l.Action,
            l.IpAddress,
            l.UserAgent,
            l.Success,
            l.Details,
            l.Timestamp
        )).ToList();
    }

    public async Task RecordAccessLogAsync(string username, string action, string ipAddress, string userAgent, bool success, string? details = null, CancellationToken ct = default)
    {
        var log = new AccessLog(username, action, ipAddress, userAgent, success, details);
        await _accessLogRepository.AddAsync(log, ct);
        await _accessLogRepository.SaveChangesAsync(ct);
    }
}
