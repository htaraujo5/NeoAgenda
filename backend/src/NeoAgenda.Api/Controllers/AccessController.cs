using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;

namespace NeoAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccessController : ControllerBase
{
    private readonly IAccessManagementService _accessService;

    public AccessController(IAccessManagementService accessService)
    {
        _accessService = accessService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _accessService.GetUsersAsync(ct);
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken ct)
    {
        var user = await _accessService.CreateUserAsync(request, ct);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var agent = Request.Headers.UserAgent.ToString();
        await _accessService.RecordAccessLogAsync(User.Identity?.Name ?? "system", "USER_CREATED", ip, agent, true, $"Criou o usuário '{user.Username}'", ct);
        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
    }

    [HttpPost("users/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeAccess(Guid id, CancellationToken ct)
    {
        await _accessService.RevokeUserAccessAsync(id, ct);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var agent = Request.Headers.UserAgent.ToString();
        await _accessService.RecordAccessLogAsync(User.Identity?.Name ?? "system", "ACCESS_REVOKED", ip, agent, true, $"Revogou tokens do usuário ID: {id}", ct);
        return NoContent();
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int count = 100, CancellationToken ct = default)
    {
        var logs = await _accessService.GetAccessLogsAsync(count, ct);
        return Ok(logs);
    }
}
