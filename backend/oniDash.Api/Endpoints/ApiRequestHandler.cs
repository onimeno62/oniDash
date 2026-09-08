using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Common;

namespace oniDash.Api.Endpoints;

/// <summary>
/// Translates application exceptions to consistent HTTP error semantics, once, for every
/// endpoint. Endpoint bodies stay thin: parse input, call the use case, wrap the result.
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
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: ex.Message);
        }
        catch (ConflictException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: ex.Message);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors);
        }
    }
}
