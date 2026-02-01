using FluentValidation;

namespace RenuMate.Api.Subscriptions.GetAllForUser;

public class GetAllSubscriptionsRequestValidator : AbstractValidator<GetAllSubscriptionsRequest>
{
    private readonly string[] _allowedSortBy = ["name", "renewaldate", "createdat"];
    private readonly string[] _allowedSortOrder = ["asc", "desc"];

    public GetAllSubscriptionsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrEmpty(value) || _allowedSortBy.Contains(value.ToLower()))
            .WithMessage($"SortBy can only be one of the following: {string.Join(", ", _allowedSortBy)}");

        RuleFor(x => x.SortOrder)
            .Must(value => string.IsNullOrEmpty(value) || _allowedSortOrder.Contains(value.ToLower()))
            .WithMessage("SortOrder must be either 'asc' or 'desc'.");
    }
}