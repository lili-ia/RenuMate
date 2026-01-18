namespace RenuMate.Api.Users.GetInfo;

public sealed record UserInfoResponse(
    Guid Id,
    string Email,
    string Name,
    DateTime MemberSince,
    int SubscriptionCount
);