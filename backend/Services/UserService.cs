using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend.Services;

public class UserService(AppDbContext db) : ApiServiceBase(db)
{
    private readonly AppDbContext DB = db;

    public async Task<User?> Create(CreateUsersDto dto)
    {
        bool token = await DB.Users.AnyAsync(u => u.Email == dto.Email);
        if (token)
            return null;
        User user = new()
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };
        DB.Users.Add(user);
        await DB.SaveChangesAsync();
        return user;
    }

    public async Task<User?> ValidateCredentials(LoginDto dto)
    {
        User?  user = await GetUserByEmail(dto.Email);

        if ( user is null || user.Password is null)
            return null;

        bool ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
        return ok ? user : null;
    }
    
    public async Task<bool> Delete(int id)
    {
        int rows = await DB.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
        return rows > 0;
    }
    
    public async Task<User?> AcceptTerms(int userId)
    {
        return await Update(userId, user => user.TermsAcceptedAt = DateTime.UtcNow);
    }
   
    public async Task<User?> ChangeName(int userId, string username)
    { 
       return await Update(userId, user => user.Username = username);
    } 

    public async Task<User?> ChangeAvatar(int userId, string avatar)
    {
        return await Update(userId, user => user.Avatar = avatar);
    } 

    public async Task<User?> ChangeLanguage(int userId, string language)
    {
        return await Update(userId, user => user.Language = language);
    }

    public async Task<User?> ChangeColor(int userId, string color)
    {
        return  await Update(userId, user => user.Color = color);
    }

    public async Task<User?> ChangeTheme(int userId, bool theme)
    {
        return  await Update(userId, user => user.Theme = theme);
    }
 
    public async Task<Translations?> GetTranslations (string key )
    {
        return await DB.Translations.FirstOrDefaultAsync(t => t.Key == key);
    }
}