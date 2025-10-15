using FluentValidation;

namespace RenuMate.Auth.RequestEmailChange;

public class EmailChangeRequest
{
    public string NewEmail { get; set; } = null!;
}

public class ChangeEmailRequestValidator : AbstractValidator<EmailChangeRequest>
{
    public ChangeEmailRequestValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("NewEmail is required.")
            .EmailAddress().WithMessage("NewEmail is not valid.");
    }
}