using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using RenuMate.Api.Common;
using RenuMate.Api.Entities;
using RenuMate.Api.Extensions;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Tags.Create;

public class CreateTagEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/tags", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Creates a new tag that can be applied to subscriptions.")
        .WithTags("Tags")
        .Produces<CreateTagResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> Handle(
        [FromBody] CreateTagRequest request,
        RenuMateDbContext db,
        IValidator<CreateTagRequest> validator,
        IUserContext userContext,
        ILogger<CreateTagEndpoint> logger,
        IMemoryCache cache,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }
        
        var exists = await db.Tags
            .AnyAsync(t => 
                (t.UserId == userId || t.IsSystem) && 
                EF.Functions.ILike(t.Name, request.Name.Trim()), cancellationToken);

        if (exists)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tag already exists",
                detail: $"Tag with name '{request.Name}' is already reserved or exists."
            );
        }

        var tag = Tag.CreateUserTag(request.Name, request.Color, userId);

        try
        {
            await db.Tags.AddAsync(tag, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException { SqlState: "23505" })
        {
            logger.LogWarning("Race condition: User {UserId} created duplicate tag {TagName}", userId, request.Name);
        
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tag already exists",
                detail: "A tag with this name was just created."
            );
        }
        
        var cacheKey = $"tags_{userId}";
        cache.Remove(cacheKey);
        
        return Results.Created(
            $"/api/tags/{tag.Id}", 
            new CreateTagResponse(tag.Id, tag.Name, tag.Color));
    }
}