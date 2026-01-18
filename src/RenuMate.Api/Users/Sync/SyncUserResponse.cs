namespace RenuMate.Api.Users.Sync;

public sealed record SyncUserResponse(
    string Message, 
    Guid UserId
);