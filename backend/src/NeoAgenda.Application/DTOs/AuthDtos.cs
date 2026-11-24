namespace NeoAgenda.Application.DTOs;

public record LoginRequestDto(string Username, string Password);

public record RefreshTokenRequestDto(string RefreshToken);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Username,
    string Name,
    string Role
);
