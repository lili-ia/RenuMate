using RenuMate.Common;

namespace RenuMate.Extensions;

public static class ResultExtensions
{
    public static IResult ToIResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value is not null
                ? Results.Ok(result.Value)
                : Results.NoContent();
        }

        return result.ErrorType switch
        {
            ErrorType.NotFound => Results.NotFound(result.ErrorMessage),
            ErrorType.Validation => Results.BadRequest(result.ErrorMessage),
            ErrorType.BadRequest => Results.BadRequest(result.ErrorMessage),
            ErrorType.Unauthorized => Results.Problem(detail: result.ErrorMessage, statusCode: 401),
            ErrorType.Forbidden => Results.Problem(detail: result.ErrorMessage, statusCode: 403),
            ErrorType.ServerError or null => Results.InternalServerError(),
            _ => Results.Problem("Unexpected error")
        };
    }

    public static IResult ToIResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return result.ErrorType switch
        {
            ErrorType.NotFound => Results.NotFound(result.ErrorMessage),
            ErrorType.Validation => Results.BadRequest(result.ErrorMessage),
            ErrorType.Forbidden => Results.Problem(detail: result.ErrorMessage, statusCode: 403),
            ErrorType.Unauthorized => Results.Problem(detail: result.ErrorMessage, statusCode: 401),
            ErrorType.ServerError or null => Results.InternalServerError(),
            _ => Results.Problem("Unexpected error")
        };
    }
}