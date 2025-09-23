using FluentValidation;

namespace RenuMate.Reminders.Update;

public class UpdateReminderRequest
{
    public int DaysBeforeRenewal { get; set; }  
    
    public TimeSpan NotifyTime { get; set; }
}

public class UpdateReminderRequestValidator : AbstractValidator<UpdateReminderRequest>
{
    public UpdateReminderRequestValidator()
    {
        RuleFor(x => x.DaysBeforeRenewal)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DaysBeforeRenewal must be zero or greater.");

        RuleFor(x => x.NotifyTime)
            .Must(t => t.TotalHours >= 0 && t.TotalHours < 24)
            .WithMessage("NotifyTime must be a valid time of day (0:00 to 23:59).");
    }
}