using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using oniDash.Application.Common;

namespace oniDash.Api.Endpoints;

/// <summary>
/// Translates application exceptions to the platform error envelope. Endpoint bodies stay
/// thin: parse input, call the use case, wrap the result.
/// </summary>
public static class ApiRequestHandler
{
    public static async Task<IResult> RunAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (NotFoundException ex)
        {
            return Error(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            return Error(StatusCodes.Status409Conflict, ApiErrorCodes.Conflict, ex.Message);
        }
        catch (ValidationException ex)
        {
            // ValidationException exposes IDictionary; the API envelope wants IReadOnlyDictionary.
            var details = ex.Errors is null ? null : new Dictionary<string, string[]>(ex.Errors);
            return Error(StatusCodes.Status400BadRequest, ApiErrorCodes.Validation, ex.Message, details);
        }
    }

    private static IResult Error(
        int statusCode,
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? details = null) =>
        Results.Json(new ApiError(code, message, null, details), statusCode: statusCode);
}
