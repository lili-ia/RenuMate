using MediatR;
using RenuMate.Api.Enums;

namespace RenuMate.Api.Subscriptions.Create;

public sealed record CreateSubscriptionCommand
(
    Guid UserId,
    string Name,
    SubscriptionPlan Plan,
    int? CustomPeriodInDays,
    int? TrialPeriodInDays,
    DateOnly StartDate,
    decimal Cost,
    Currency Currency,
    List<Guid> TagIds,
    string? Note,
    string? CancelLink,
    string? PicLink
) : IRequest<IResult>;