using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;
using NeoAgenda.Domain.Common;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Infrastructure.Security;

namespace NeoAgenda.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, ct);
        if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new DomainException("Credenciais inválidas. Verifique o usuário e senha informados.");
        }

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new DomainException("O Refresh Token é obrigatório.");
        }

        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, ct);
        if (user == null || user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            throw new DomainException("Refresh Token inválido ou expirado. Faça login novamente.");
        }

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task RevokeTokenAsync(string username, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, ct);
        if (user != null)
        {
            user.RevokeRefreshToken();
            await _userRepository.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user, CancellationToken ct)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "SuperSecretKeyForNeoAgendaSecureAuthentication2026!";
        var issuer = _configuration["Jwt:Issuer"] ?? "NeoAgenda";
        var audience = _configuration["Jwt:Audience"] ?? "NeoAgendaClient";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60; // 60 minutos

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.Name),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        // Gera novo Refresh Token (válido por 7 dias)
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        user.SetRefreshToken(refreshToken, refreshTokenExpiresAt);
        await _userRepository.SaveChangesAsync(ct);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: tokenDescriptor.Expires!.Value,
            Username: user.Username,
            Name: user.Name,
            Role: user.Role
        );
    }
}
