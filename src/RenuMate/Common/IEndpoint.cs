namespace RenuMate.Common;

public interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}