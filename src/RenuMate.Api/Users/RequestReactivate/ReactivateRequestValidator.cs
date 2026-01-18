using FluentValidation;

namespace RenuMate.Api.Users.RequestReactivate;

public class ReactivateRequestValidator : AbstractValidator<ReactivateUserRequest>
{
    public ReactivateRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
    }
}