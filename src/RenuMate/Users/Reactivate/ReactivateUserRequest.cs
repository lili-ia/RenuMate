using FluentValidation;

namespace RenuMate.Users.Reactivate;

public class ReactivateUserRequest
{
    public string Token { get; set; } = null!;
}

public class ReactivateUserRequestValidator : AbstractValidator<ReactivateUserRequest>
{
    public ReactivateUserRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MinimumLength(20).WithMessage("Token looks too short.");
    }
}