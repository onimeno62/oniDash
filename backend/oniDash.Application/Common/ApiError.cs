namespace oniDash.Application.Common;

/// <summary>
/// Stable, machine-readable API failure contract. HTTP status remains the transport-level
/// classification; Code is the application-level classification consumed by typed clients.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string[]>? Details = null);

public static class ApiErrorCodes
{
    public const string Validation = "validation_error";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string Forbidden = "forbidden";
    public const string Unauthorized = "unauthorized";
    public const string Internal = "internal_error";
    public const string Cancelled = "cancelled";
}
