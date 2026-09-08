/**
 * Typed transport for the oniDash HTTP API.
 *
 * The frontend never touches SQLite or the filesystem directly (AGENTS.md rule 3): every
 * interaction goes through this client. In development, Vite proxies `/api` to the local
 * API host; in production the API serves the built SPA from the same origin.
 */

const API_BASE: string = import.meta.env.VITE_API_BASE ?? '/api';

/** Absolute path for an API-relative route, honoring the configured base. */
export function apiUrl(path: string): string {
  return `${API_BASE}${path}`;
}

/** Error raised for transport failures and non-2xx API responses. */
export class ApiError extends Error {
  readonly status: number;
  /** Field errors from RFC 7807 validation problems, when the API sends them. */
  readonly errors: Record<string, string[]> | null;

  constructor(
    message: string,
    status: number,
    options?: { errors?: Record<string, string[]> | null; cause?: unknown },
  ) {
    super(message, { cause: options?.cause });
    this.name = 'ApiError';
    this.status = status;
    this.errors = options?.errors ?? null;
  }
}

/**
 * Extracts a user-presentable message from an RFC 7807 problem response:
 * `detail` for domain errors ("already exists", "folder does not exist"), the first
 * validation message for 400s, or the HTTP status as a fallback.
 */
async function problemMessage(response: Response): Promise<{
  message: string;
  errors: Record<string, string[]> | null;
}> {
  try {
    const problem = (await response.json()) as {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    };

    const validationMessage = problem.errors
      ? Object.values(problem.errors).flat()[0]
      : undefined;

    return {
      message: problem.detail ?? validationMessage ?? problem.title ?? `HTTP ${response.status}`,
      errors: problem.errors ?? null,
    };
  } catch {
    return { message: `HTTP ${response.status}`, errors: null };
  }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, init);
  } catch (cause) {
    throw new ApiError('The oniDash API is unreachable.', 0, { cause });
  }

  if (!response.ok) {
    const problem = await problemMessage(response);
    throw new ApiError(problem.message, response.status, { errors: problem.errors });
  }

  return (await response.json()) as T;
}

/** DELETE requests that return 204 No Content (no body to parse). */
export async function apiDelete(path: string): Promise<void> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, { method: 'DELETE' });
  } catch (cause) {
    throw new ApiError('The oniDash API is unreachable.', 0, { cause });
  }

  if (!response.ok) {
    const problem = await problemMessage(response);
    throw new ApiError(problem.message, response.status, { errors: problem.errors });
  }
}
