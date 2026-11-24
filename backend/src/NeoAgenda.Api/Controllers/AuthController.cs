using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;

namespace NeoAgenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAccessManagementService _accessService;

    public AuthController(IAuthService authService, IAccessManagementService accessService)
    {
        _authService = authService;
        _accessService = accessService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var agent = Request.Headers.UserAgent.ToString();

        try
        {
            var response = await _authService.LoginAsync(request, ct);
            await _accessService.RecordAccessLogAsync(request.Username, "LOGIN_SUCCESS", ip, agent, true, "Autenticação via senha bem-sucedida", ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            await _accessService.RecordAccessLogAsync(request.Username, "LOGIN_FAILED", ip, agent, false, ex.Message, ct);
            throw;
        }
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var agent = Request.Headers.UserAgent.ToString();

        try
        {
            var response = await _authService.RefreshTokenAsync(request, ct);
            await _accessService.RecordAccessLogAsync(response.Username, "TOKEN_REFRESH", ip, agent, true, "Token de acesso renovado com sucesso", ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            await _accessService.RecordAccessLogAsync("unknown", "TOKEN_REFRESH_FAILED", ip, agent, false, ex.Message, ct);
            throw;
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var username = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(username))
        {
            await _authService.RevokeTokenAsync(username, ct);
        }
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            Username = User.Identity?.Name,
            IsAuthenticated = true,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}
