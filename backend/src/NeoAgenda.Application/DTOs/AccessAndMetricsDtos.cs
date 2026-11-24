namespace NeoAgenda.Application.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Name,
    string Role,
    DateTime CreatedAt,
    bool HasActiveSession
);

public record CreateUserRequestDto(
    string Username,
    string Password,
    string Name,
    string Role
);

public record AccessLogDto(
    Guid Id,
    string Username,
    string Action,
    string IpAddress,
    string UserAgent,
    bool Success,
    string? Details,
    DateTime Timestamp
);

public record PerformanceSummaryDto(
    long TotalRequests,
    double AverageDurationMs,
    long P95DurationMs,
    long MaxDurationMs,
    int ErrorCount,
    double ErrorRatePercentage,
    IReadOnlyList<EndpointMetricDto> TopSlowestEndpoints,
    IReadOnlyList<StatusCodeDistributionDto> StatusCodeDistribution
);

public record EndpointMetricDto(
    string Method,
    string Path,
    long TotalCalls,
    double AverageDurationMs,
    long MaxDurationMs
);

public record StatusCodeDistributionDto(
    int StatusCode,
    long Count
);
