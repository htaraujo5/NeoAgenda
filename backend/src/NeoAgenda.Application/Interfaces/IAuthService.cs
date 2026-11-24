using NeoAgenda.Application.DTOs;

namespace NeoAgenda.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default);
    Task RevokeTokenAsync(string username, CancellationToken ct = default);
}
