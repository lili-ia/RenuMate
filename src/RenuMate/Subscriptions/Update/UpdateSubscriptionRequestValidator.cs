using FluentValidation;
using RenuMate.Enums;

namespace RenuMate.Subscriptions.Update;

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subscription name is required.")
            .MaximumLength(100).WithMessage("Subscription name must not exceed 100 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Subscription type is required.")
            .Must(type => Enum.TryParse<SubscriptionType>(type, true, out _))
            .WithMessage("Invalid subscription type.");

        RuleFor(x => x.CustomPeriodInDays)
            .GreaterThan(0)
            .When(x => x.Type.Equals(nameof(SubscriptionType.Custom), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Custom period must be greater than zero for custom subscriptions.")
            .Null()
            .When(x => !x.Type.Equals(nameof(SubscriptionType.Custom), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Custom period should only be set for custom subscriptions.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(5))
            .WithMessage("Start date is too far in the future.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost must be non-negative.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Must(currency => Enum.TryParse<Currency>(currency, true, out _))
            .WithMessage("Invalid currency.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}