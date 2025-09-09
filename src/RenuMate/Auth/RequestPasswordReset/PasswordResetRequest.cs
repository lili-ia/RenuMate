using FluentValidation;

namespace RenuMate.Auth.RequestPasswordReset;

public class PasswordResetRequest
{
    public string Email { get; set; } = null!;
}

public class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequest>
{
    public PasswordResetRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
    }
}