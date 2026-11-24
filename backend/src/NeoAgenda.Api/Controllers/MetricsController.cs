using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;

namespace NeoAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    private readonly IPerformanceMetricsService _metricsService;

    public MetricsController(IPerformanceMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _metricsService.GetPerformanceSummaryAsync(ct);
        return Ok(summary);
    }
}
