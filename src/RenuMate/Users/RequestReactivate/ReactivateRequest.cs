using FluentValidation;

namespace RenuMate.Users.RequestReactivate;

public class ReactivateRequest
{
    public string Email { get; set; }
}

public class ReactivateRequestValidator : AbstractValidator<ReactivateRequest>
{
    public ReactivateRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
    }
}