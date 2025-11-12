namespace RenuMate.Users.GetInfo;

public class UserInfoResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    
    public DateTime MemberSince { get; set; }
    
    public int SubscriptionCount { get; set; }
}