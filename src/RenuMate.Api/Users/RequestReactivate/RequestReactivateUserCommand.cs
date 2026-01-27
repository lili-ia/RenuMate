using MediatR;

namespace RenuMate.Api.Users.RequestReactivate;

public sealed record RequestReactivateUserCommand(Guid UserId) : IRequest<IResult>;