using FluentValidation;

namespace RenuMate.Reminders.Create;

public class CreateReminderRequestValidator : AbstractValidator<CreateReminderRequest>
{
    public CreateReminderRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty()
            .WithMessage("SubscriptionId is required");
        
        RuleFor(x => x.DaysBeforeRenewal)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DaysBeforeRenewal must be zero or greater.");

        RuleFor(x => x.NotifyTime)
            .Must(t => t.TotalHours >= 0 && t.TotalHours < 24)
            .WithMessage("NotifyTime must be a valid time of day (0:00 to 23:59).");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.");
    }
}