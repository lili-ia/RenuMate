using FluentValidation;

namespace RenuMate.Auth;

public class TokenValidator : AbstractValidator<string>
{
    public TokenValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Token is required.")
            .MinimumLength(20).WithMessage("Token looks too short.");
    }
}