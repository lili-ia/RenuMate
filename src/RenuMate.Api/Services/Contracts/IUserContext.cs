namespace RenuMate.Api.Services.Contracts;

public interface IUserContext
{
    Guid UserId { get; }
    
    string Auth0Id { get; }
}