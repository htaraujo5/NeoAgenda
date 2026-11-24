export interface UserDto {
  id: string;
  username: string;
  name: string;
  role: string;
  createdAt: string;
  hasActiveSession: boolean;
}

export interface CreateUserRequestDto {
  username: string;
  password: string;
  name: string;
  role: string;
}

export interface AccessLogDto {
  id: string;
  username: string;
  action: string;
  ipAddress: string;
  userAgent: string;
  success: boolean;
  details: string | null;
  timestamp: string;
}

export interface EndpointMetricDto {
  method: string;
  path: string;
  totalCalls: number;
  averageDurationMs: number;
  maxDurationMs: number;
}

export interface StatusCodeDistributionDto {
  statusCode: number;
  count: number;
}

export interface PerformanceSummaryDto {
  totalRequests: number;
  averageDurationMs: number;
  p95DurationMs: number;
  maxDurationMs: number;
  errorCount: number;
  errorRatePercentage: number;
  topSlowestEndpoints: EndpointMetricDto[];
  statusCodeDistribution: StatusCodeDistributionDto[];
}
