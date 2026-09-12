using Microsoft.AspNetCore.Mvc;
using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Backend.Options;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]

public class OAuthController (OAuthService auth, IOptions<GoogleOptions> google, IOptions<GithubOptions> github) : ApiControllerBase 
{
    private async Task<IActionResult> Redirect (AuthResultDto? result)
    {
        const string Url = "http://localhost:3000/";
        if (result is null)
            return Redirect(Url + "server/login");
        string path = QueryHelpers.AddQueryString(Url + "api/auth",
        new Dictionary<string, string?>
        {
            ["accessToken"] = result.AccessToken,
            ["refreshToken"] = result.RefreshToken,
        });
        return Redirect(path);
    }

    private static Dictionary<string, string?> CreateDictionary(IOAuthSettings settings)
    {
        return new Dictionary<string, string?>
        {
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = settings.CallbackUrl,
            ["response_type"] = "code",
        };
    }

    [HttpGet("google")]
    public  IActionResult Google()
    {
        Dictionary<string, string?> body = CreateDictionary(google.Value);
        body.Add("scope", "openid email profile");
        string url = QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", body);
        return Redirect(url);
    }
    
    [HttpGet("github")]
    public IActionResult Github()
    {
        Dictionary<string, string?> body = CreateDictionary(github.Value);
        body.Add("scope", "read:user user:email");
        string url = QueryHelpers.AddQueryString("https://github.com/login/oauth/authorize", body);
        return Redirect(url);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback ([FromQuery] string code)
    {
        AuthResultDto? result = await auth.GoogleLogin(code);
        return await Redirect(result);
    }
    [HttpGet("github/callback")]
    public async Task<IActionResult> GithubCallback([FromQuery] string code)
    {
        AuthResultDto? result = await auth.GithubLogin(code);
        return await Redirect(result);
    }

}