using FluentValidation;

namespace RenuMate.Auth.ResendEmailConfirmation;

public class ResendEmailConfirmationRequest
{
    public string Email { get; set; } = null!;
}

public class ResendEmailConfirmationRequestValidator : AbstractValidator<ResendEmailConfirmationRequest>
{
    public ResendEmailConfirmationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
    }
}