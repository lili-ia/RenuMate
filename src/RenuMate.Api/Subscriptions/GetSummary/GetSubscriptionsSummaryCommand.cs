using MediatR;
using RenuMate.Api.Enums;

namespace RenuMate.Api.Subscriptions.GetSummary;

public sealed record GetSubscriptionsSummaryCommand(
    Guid UserId, 
    Currency Currency, 
    string Period) : IRequest<IResult>;