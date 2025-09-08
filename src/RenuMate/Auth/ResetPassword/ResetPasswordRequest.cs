using FluentValidation;

namespace RenuMate.Auth.ResetPassword;

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = null!;
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("NewPassword is required.")
            .MinimumLength(6).WithMessage("NewPassword must be at least 6 characters.");
    }
}