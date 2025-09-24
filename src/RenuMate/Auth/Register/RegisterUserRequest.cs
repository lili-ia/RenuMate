using FluentValidation;

namespace RenuMate.Auth.Register;

public class RegisterUserRequest
{
    public string Email { get; set; } = null!;
    
    public string Name { get; set; }

    public string Password { get; set; } = null!;
}

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(50).WithMessage("Max Name length is 50.")
            .MinimumLength(2).WithMessage("Min Name length is 2.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}