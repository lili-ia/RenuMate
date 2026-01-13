using FluentValidation;

namespace RenuMate.Users.RequestReactivate;

public class ReactivateRequestValidator : AbstractValidator<ReactivateUserRequest>
{
    public ReactivateRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
    }
}