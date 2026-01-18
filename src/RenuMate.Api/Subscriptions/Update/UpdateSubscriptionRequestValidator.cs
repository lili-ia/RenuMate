using FluentValidation;
using RenuMate.Api.Enums;

namespace RenuMate.Api.Subscriptions.Update;

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subscription name is required.")
            .MaximumLength(100).WithMessage("Subscription name must not exceed 100 characters.");

        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Subscription plan is required.")
            .Must(plan => Enum.TryParse<SubscriptionPlan>(plan, true, out _))
            .WithMessage("Invalid subscription plan.");

        RuleFor(x => x.CustomPeriodInDays)
            .GreaterThan(0)
            .When(x => x.Plan.Equals(nameof(SubscriptionPlan.Custom), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Custom period must be greater than zero for custom subscriptions.")
            .Null()
            .When(x => !x.Plan.Equals(nameof(SubscriptionPlan.Custom), StringComparison.OrdinalIgnoreCase))
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
        
        RuleFor(x => x.CancelLink)
            .Must(link => string.IsNullOrWhiteSpace(link) || Uri.IsWellFormedUriString(link, UriKind.Absolute))
            .WithMessage("Cancel link must be a valid absolute URL if provided.");
        
        RuleFor(x => x.PicLink)
            .Must(link => string.IsNullOrWhiteSpace(link) || Uri.IsWellFormedUriString(link, UriKind.Absolute))
            .WithMessage("Pic link must be a valid absolute URL if provided.");
    }
}