using MediatR;

namespace RenuMate.Api.Users.Sync;

public record SyncUserCommand(string Auth0Id, string Email, string Name, bool IsVerified) : IRequest<IResult>;