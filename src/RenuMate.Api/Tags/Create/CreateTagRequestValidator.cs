using FluentValidation;

namespace RenuMate.Api.Tags.Create;

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(50).WithMessage("Tag name must not exceed 50 characters.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Tag name cannot consist of spaces only.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .WithMessage("Color must be a valid HEX format (e.g., #FFFFFF or #FFF).")
            .MaximumLength(10).WithMessage("Color string is too long.");
    }
}