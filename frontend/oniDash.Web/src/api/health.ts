import { ApiError, apiUrl } from './client';

/** Aggregate application health (mirrors oniDash.Application.AppHealthReport). */
export type AppHealthStatus = 'Healthy' | 'Unhealthy';

/** Database probe outcome (mirrors oniDash.Application.DatabaseHealthStatus). */
export type DatabaseHealthStatus = 'Ok' | 'Unavailable';

export interface AppHealthReport {
  status: AppHealthStatus;
  version: string;
  databaseStatus: DatabaseHealthStatus;
  /** UTC instant the report was produced (ISO 8601). */
  timestamp: string;
}

export async function fetchHealth(signal?: AbortSignal): Promise<AppHealthReport> {
  let response: Response;
  try {
    response = await fetch(apiUrl('/health'), { signal });
  } catch (cause) {
    throw new ApiError('The oniDash API is unreachable.', 0, { cause });
  }

  // 503 still carries a health report body: the API is up but degraded, which is
  // exactly what the health page should display instead of an unreachable error.
  if (response.ok || response.status === 503) {
    return (await response.json()) as AppHealthReport;
  }

  throw new ApiError(`The oniDash API responded with HTTP ${response.status}.`, response.status);
}
