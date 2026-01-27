using FluentValidation;

namespace RenuMate.Api.Users.ConfirmReactivate;

public class ConfirmReactivateUserRequestValidator : AbstractValidator<ConfirmReactivateUserRequest>
{
    public ConfirmReactivateUserRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MinimumLength(20).WithMessage("Token looks too short.");
    }
}