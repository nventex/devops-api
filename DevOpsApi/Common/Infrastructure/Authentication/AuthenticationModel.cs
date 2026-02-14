namespace DevOpsApi.Common.Infrastructure.Authentication;

public class AuthenticationModel
{
    public string AccessToken { get; set; }

    public string User { get; set; }

    public bool IsAuthenticated { get; set; }
    
    public void Authenticated(string user)
    {
        User = user;
        IsAuthenticated = true;
    }
}
