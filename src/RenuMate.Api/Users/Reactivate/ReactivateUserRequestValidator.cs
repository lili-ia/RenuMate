using FluentValidation;

namespace RenuMate.Api.Users.Reactivate;

public class ReactivateUserRequestValidator : AbstractValidator<ReactivateUserRequest>
{
    public ReactivateUserRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MinimumLength(20).WithMessage("Token looks too short.");
    }
}