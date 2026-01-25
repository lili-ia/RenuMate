using MediatR;

namespace RenuMate.Api.Users.Reactivate;

public sealed record ReactivateUserCommand(string Token) : IRequest<IResult>;