namespace RenuMate.Users.Sync;

public sealed record SyncUserResponse(
    string Message, 
    Guid UserId
);