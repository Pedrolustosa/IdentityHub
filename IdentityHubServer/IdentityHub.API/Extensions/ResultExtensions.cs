using IdentityHub.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace IdentityHub.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        var statusCode = ResolveStatusCode(result.Error?.Code);

        return new ObjectResult(new
        {
            error = result.Error!.Code,
            message = result.Error.Message
        })
        {
            StatusCode = (int)statusCode
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        var statusCode = ResolveStatusCode(result.Error?.Code);

        return new ObjectResult(new
        {
            error = result.Error!.Code,
            message = result.Error.Message
        })
        {
            StatusCode = (int)statusCode
        };
    }

    private static HttpStatusCode ResolveStatusCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return HttpStatusCode.BadRequest;

        if (errorCode.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase))
            return HttpStatusCode.NotFound;

        if (errorCode.StartsWith("Auth.", StringComparison.OrdinalIgnoreCase))
        {
            if (errorCode.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
                return HttpStatusCode.Forbidden;

            return HttpStatusCode.Unauthorized;
        }

        return HttpStatusCode.BadRequest;
    }
}