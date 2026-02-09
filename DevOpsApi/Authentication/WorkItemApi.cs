using DevOpsApi.Common.Infrastructure.Authentication;
using LazyCache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DevOpsApi.Authentication;

public static class AuthenticationApi
{
    public static void MapAuthentication(this WebApplication app)
    {
        app.MapGet("/authentication", ([FromHeader(Name = "Authorization")] string token, IAppCache cache, CancellationToken cancellationToken) =>
            {
                cache.Add("access-token", new AuthenticationModel { AccessToken = token }, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(25) });
                return Task.CompletedTask;
                
            }).WithName("CacheAccessToken");
    }
}