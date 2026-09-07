using Backend.Dtos;
using Backend.Models;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class AuthService(UserService users, TokenService tokens, AppDbContext DB)
{
    private readonly UserService _users = users;
    private readonly TokenService _tokens = tokens;
    private readonly AppDbContext _db = DB;

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

    public async Task Logout(int userId)
    {
        var sessions = await _db.Sessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync();
        foreach (var s in sessions)
            s.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
    public  Task<bool> DeleteAccount (int userId)
    {
        return _users.Delete(userId);
    }
}