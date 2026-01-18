using FluentValidation.Results;

namespace RenuMate.Api.Extensions;

public static class ValidationResultExtensions
{
    public static IResult ToFailureResult(this ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return Results.ValidationProblem(errors);
    }
}