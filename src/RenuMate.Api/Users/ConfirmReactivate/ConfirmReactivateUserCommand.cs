using MediatR;

namespace RenuMate.Api.Users.ConfirmReactivate;

public sealed record ConfirmReactivateUserCommand(string Token) : IRequest<IResult>;