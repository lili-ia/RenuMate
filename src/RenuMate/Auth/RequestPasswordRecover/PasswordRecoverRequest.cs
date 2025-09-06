using FluentValidation;

namespace RenuMate.Auth.RequestPasswordRecover;

public class PasswordRecoverRequest
{
    public string Email { get; set; }
}

public class PasswordRecoverRequestValidator : AbstractValidator<PasswordRecoverRequest>
{
    public PasswordRecoverRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
    }
}