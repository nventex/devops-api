using DevOpsApi.Common.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsApi.Authentication;

public static class AuthenticationApi
{
    public static void MapAuthentication(this WebApplication app)
    {
        app.MapGet("/authentication", async ([FromHeader(Name = "Authorization")] string token, AuthenticationHandler handler, CancellationToken cancellationToken) =>
        {
                var model = await handler.Handle(new AuthenticationModel { AccessToken = token }, cancellationToken);

                return model.IsAuthenticated ? Task.CompletedTask : throw new UnauthorizedAccessException();
        }).WithName("CacheAccessToken");
    }
}