namespace RenuMate.Users.Sync;

public class SyncUserResponse
{
    public string Message { get; set; } = null!;
    
    public Guid UserId { get; set; }
}