using Microsoft.Extensions.Caching.Memory;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Middleware;

public class InvalidateSummaryCacheEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (context.HttpContext.Response.StatusCode is < 200 or >= 300)
        {
            return result;
        }
        
        var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
        var userContext = context.HttpContext.RequestServices.GetService<IUserContext>();

        if (userContext is null || cache is null)
        {
            return result;
        }
        
        var signalKey = $"signal_{userContext.UserId}";

        if (!cache.TryGetValue(signalKey, out CancellationTokenSource? cts) || cts is null)
        {
            return result;
        }

        cts.Cancel();
        cts.Dispose();
        cache.Remove(signalKey);

        return result;
    }
}