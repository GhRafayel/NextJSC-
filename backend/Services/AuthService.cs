using Backend.Dtos;
using Backend.Models;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class AuthService(UserService users, TokenService tokens, EmailService email, AppDbContext db) : ApiServiceBase(db)
{
    private readonly UserService _users = users;
    private readonly TokenService _tokens = tokens;
    private readonly AppDbContext _db = db;
    private readonly EmailService _emil = email;

    public async Task<AuthResultDto?> Register(CreateUsersDto dto)
    {
        User? user = await _users.Create(dto);
        if (user is null)
            return null;

        return await IssueTokens(user);
    }

    public async Task<AuthResultDto?> Login(LoginDto dto)
    {
        User? user = await _users.ValidateCredentials(dto);
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
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return new AuthResultDto
        {
            AccessToken = access,
            RefreshToken = refresh,
        };
    }

    public async Task<AuthResultDto?> Refresh(RefreshDto dto)
    {
        var session = await _db.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.RefreshToken == dto.RefreshToken);
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
        await _db.SaveChangesAsync();
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
            await _db.SaveChangesAsync();
            return null;
        }
    }

    public async Task Logout(int userId)
    {
        var sessions = await _db.Sessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync();
        foreach (var s in sessions)
            s.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
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
            await _db.SaveChangesAsync();
            return false;
        }
        
        user.Password = user.NewPassword;
        user.NewPassword = null;
        user.CodeExpire = null;
        user.ResetCode = null;
        user.ResetCodeAttempts = 0;
        await _db.SaveChangesAsync();
        await Logout(user.Id);
        return true;
    }

    public  Task<bool> DeleteAccount (int userId)
    {
        return _users.Delete(userId);
    }
}