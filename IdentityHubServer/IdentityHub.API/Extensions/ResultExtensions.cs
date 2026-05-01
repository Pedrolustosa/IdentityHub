using IdentityHub.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        return new BadRequestObjectResult(new
        {
            error = result.Error!.Code,
            message = result.Error.Message
        });
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return new BadRequestObjectResult(new
        {
            error = result.Error!.Code,
            message = result.Error.Message
        });
    }
}