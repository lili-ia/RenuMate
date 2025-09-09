using FluentValidation.Results;

namespace RenuMate.Extensions;

public static class ValidationResultExtensions
{
    public static IResult ToFailureResult(this ValidationResult validationResult)
    {
        var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));

        return Results.BadRequest(errorMessages);
    }
}