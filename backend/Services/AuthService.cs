using Backend.Dtos;
using Backend.Models;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class AuthService (EmailService email, AppDbContext db, TokenService token) : ApiServiceBase(db, token)
{
    private readonly EmailService _emil = email;
    
    public async Task<AuthResultDto?> Register(CreateUsersDto dto)
    {
        User? user = await Create(dto);
        if (user is null) 
            return null;
        return await IssueTokens(user);
    }

    public async Task<AuthResultDto?> Login(LoginDto dto)
    {
        User? user = await ValidateCredentials(dto);
        if (user is null)
            return null;
        return await IssueTokens(user);
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