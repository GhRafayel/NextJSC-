
using Backend.Dtos;
using Backend.Models;
using Backend.Data;
using Microsoft.Extensions.Options;
using Backend.Options;
using System.Net.Http.Headers;

namespace Backend.Services;
public class OAuthService ( IOptions<GoogleOptions> google, IOptions<GithubOptions> github, HttpClient http, AppDbContext db , TokenService token) : ApiServiceBase(db, token)
{
    private readonly HttpClient _http = http;

    private static Dictionary <string, string> CreateDictionary (string code, IOAuthSettings settings)
    {
        return new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = settings.CallbackUrl
        };
    }
    
    private async Task<string?> GetGithubPrimaryEmail(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("Backend-App");

        HttpResponseMessage response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        List<GitHubEmailDto>? emails = await response.Content.ReadFromJsonAsync<List<GitHubEmailDto>>();
        return emails?.FirstOrDefault(e => e.Primary)?.Email;
    }

    private async Task<GoogleUserInfoDto?> GetGoogleUser(string accessToken)
    {
        using var request = new HttpRequestMessage ( 
            HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo" 
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<GoogleUserInfoDto>();
    }

    private async Task<GitHubUserInfoDto?> GetGitHubUser( string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("Backend-App");
        HttpResponseMessage response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<GitHubUserInfoDto>();
    }
    
    private static ExternalUserInfo ToExternal (GoogleUserInfoDto google) => new ()
    {
        Provider = "google",
        ProviderUserId = google.Id.ToString(),
        Email = google.Email,
        Name = google.Name,
        AvatarUrl = google.Picture
    };
    
    private static ExternalUserInfo ToExternal(GitHubUserInfoDto github) => new()
    {
        Provider = "github",
        ProviderUserId = github.Id.ToString(),
        Email = github.Email,
        Name = github.Name ?? github.Login,
        AvatarUrl = github.AvatarUrl,
    };

    public async Task<AuthResultDto?> GoogleLogin (string code)
    {
        var form = CreateDictionary(code, google.Value);
        HttpResponseMessage response = await _http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form));
        if (!response.IsSuccessStatusCode)
            return null;

        OAuthTokenResponse? token =  await response.Content.ReadFromJsonAsync<OAuthTokenResponse>();
        if (token is null) return null;

        GoogleUserInfoDto? info = await GetGoogleUser(token.AccessToken);
        if (info is null) return null;

        User? user = await Create(ToExternal(info));
        return user is null ? null : await IssueTokens(user);
    }
    
    public async Task<AuthResultDto?> GithubLogin(string code)
    {
        var form = CreateDictionary(code, github.Value);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) 
            return null;

        OAuthTokenResponse? token =  await response.Content.ReadFromJsonAsync<OAuthTokenResponse>();
        if (token is null) return null;

        GitHubUserInfoDto? info = await GetGitHubUser(token.AccessToken);
        if (info is  null)
            return null;
        if (info.Email is null)
            info.Email = await GetGithubPrimaryEmail(token.AccessToken);

        User? user = await Create(ToExternal(info));
        return user is null ? null : await IssueTokens(user);
    }
   
}