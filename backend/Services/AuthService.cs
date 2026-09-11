using Backend.Dtos;
using Backend.Models;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Backend.Options;
using System.Net.Http.Headers;

namespace Backend.Services;

public class AuthService (
        TokenService tokens,
        EmailService email,
        AppDbContext db,
        HttpClient http,
        IOptions<GoogleOptions> google
    ) : ApiServiceBase(db)
{
    private readonly TokenService _tokens = tokens;
    private readonly EmailService _emil = email;
    private readonly HttpClient _http = http;
    private readonly GoogleOptions _google = google.Value;

    private async Task<GoogleTokenResponse?> ExchangeGoogleCode(string code)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _google.ClientId,
            ["client_secret"] = _google.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _google.CallbackUrl,
        };

        HttpResponseMessage response = await _http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form));
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
    }
    
    private async Task<GoogleUserInfo?> GetGoogleUser(string accessToken)
    {
        using var request = new HttpRequestMessage ( 
            HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo" 
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
    }
    
    public async Task<AuthResultDto?> Register(CreateUsersDto dto)
    {
        User? user = await Create(dto);
        if (user is null) 
            return null;
        return await IssueTokens(user);
    }

    public async Task<AuthResultDto?> GoogleLogin (string code)
    {
        GoogleTokenResponse? token = await ExchangeGoogleCode(code);
        Console.WriteLine($"[Google] token null? {token is null}");

        if (token is null)
            return null;
        GoogleUserInfo? info = await GetGoogleUser(token.AccessToken);
        Console.WriteLine($"[Google] info null? {info is null}, email={info?.Email}");

        if (info is null)
            return null;
        User user = await Create(info);
        return await IssueTokens(user);
    }
    
    public async Task<AuthResultDto?> Login(LoginDto dto)
    {
        User? user = await ValidateCredentials(dto);
        if (user is null)
            return null;
        return await IssueTokens(user);
    } 

    public async Task<AuthResultDto> IssueTokens(User user)
    {
        string access = _tokens.CreateAccessToken(user);
        var (refresh, expiresAt) = _tokens.CreateRefreshToken();
        var session = new Session
        {
            UserId = user.Id,
            RefreshToken = refresh,
            ExpiresAt = expiresAt,
        };
        DB.Sessions.Add(session);
        await DB.SaveChangesAsync();
        return new AuthResultDto
        {
            AccessToken = access,
            RefreshToken = refresh,
        };
    }

    public async Task<AuthResultDto?> Refresh(RefreshDto dto)
    {
        var session = await DB.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.RefreshToken == dto.RefreshToken);
        if (session is null || session.RevokedAt is not null || session.ExpiresAt < DateTime.UtcNow)
            return null;
        session.RevokedAt = DateTime.UtcNow;
        return await IssueTokens(session.User);
    }

    public async Task<string?> Reset(ResetPasswordDto dto)
    {
        User? user = await GetUserByEmail(dto.Email);
        if (user is null)
            return null;
        string resetCode = new Random().Next(100000, 1000000).ToString();
        string NewPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
      
        user.CodeExpire = DateTime.UtcNow.AddMinutes(15);
        user.NewPassword = NewPassword;
        user.ResetCode = resetCode;
        await DB.SaveChangesAsync();
        try
        {
            await _emil.SendAsync(user.Email, "Reset code", resetCode);
            return user.Email;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Email failed: {ex.Message}");
            user.CodeExpire = null;
            user.NewPassword = null;
            user.ResetCode = null;
            user.ResetCodeAttempts = 0;
            await DB.SaveChangesAsync();
            return null;
        }
    }

    public async Task Logout(int userId)
    {
        var sessions = await DB.Sessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync();
        foreach (var s in sessions)
            s.RevokedAt = DateTime.UtcNow;
        await DB.SaveChangesAsync();
    }
    
    public async Task<bool?> ResetCode(ResetCodeDto dto)
    {
        User? user = await GetUserByEmail(dto.Email);
        if (user is null || user.CodeExpire is null || user.CodeExpire <= DateTime.UtcNow) return null;

        if (user.ResetCode != dto.Code)
        {
            if (user.ResetCodeAttempts >= 3)
            {
                user.ResetCodeAttempts = 0;
                user.NewPassword = null;
                user.CodeExpire = null;
                user.ResetCode = null;
            }
            else
                user.ResetCodeAttempts++;
            await DB.SaveChangesAsync();
            return false;
        }
        
        user.Password = user.NewPassword;
        user.NewPassword = null;
        user.CodeExpire = null;
        user.ResetCode = null;
        user.ResetCodeAttempts = 0;
        await DB.SaveChangesAsync();
        await Logout(user.Id);
        return true;
    }

    public  Task<bool> DeleteAccount (int userId)
    {
        return DeleteUser(userId);
    }
}