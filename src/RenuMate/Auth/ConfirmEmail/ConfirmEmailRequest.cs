using FluentValidation;

namespace RenuMate.Auth.ConfirmEmail;

public class ConfirmEmailRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MinimumLength(20).WithMessage("Token looks too short.");
    }
}
