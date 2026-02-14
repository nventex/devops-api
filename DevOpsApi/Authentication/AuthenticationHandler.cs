using System.IdentityModel.Tokens.Jwt;
using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.Common.Settings;
using LazyCache;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace DevOpsApi.Authentication;

public class AuthenticationHandler
{
    private readonly IAppCache _cache;
    private readonly DevOpsSettings _options;

    public AuthenticationHandler(IOptions<DevOpsSettings> options, IAppCache cache)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<AuthenticationModel> Handle(AuthenticationModel model, CancellationToken cancellationToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var jwt = tokenHandler.ReadJwtToken(model.AccessToken);
        var user = jwt.Claims.FirstOrDefault(x => x.Type == "upn")?.Value;
        
        if (string.IsNullOrEmpty(user)) return new AuthenticationModel();

        var authModel = await _cache.GetAsync<AuthenticationModel>(user) ?? new AuthenticationModel();

        return authModel.IsAuthenticated switch
        {
            true => authModel,
            false => await Connect(user, model, cancellationToken)
        };
    }

    private async Task<AuthenticationModel> Connect(string user, AuthenticationModel model, CancellationToken cancellationToken)
    {
        try
        {
            var credentials = new VssOAuthAccessTokenCredential(model.AccessToken);
            var connection = new VssConnection(new Uri($"https://dev.azure.com/{_options.Organization}"), credentials);
            
            await connection.ConnectAsync(cancellationToken);

            if (!connection.HasAuthenticated || string.IsNullOrEmpty(user)) return new AuthenticationModel();
            
            model.Authenticated(user);
            
            _cache.Add(user, model, TimeSpan.FromHours(1));

            return model;
        }
        catch (Exception)
        {
            return new AuthenticationModel();
        }
    }
}