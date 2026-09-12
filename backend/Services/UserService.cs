using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend.Services;

public class UserService(AppDbContext db, TokenService token) : ApiServiceBase(db, token)
{
    public async Task<List<User>?> Search (int id, string name)
    {
        List<User> users = (await GetAllUsers())
            .Where(user => user.Id != id && user.Username.Contains(name)).ToList();
        return users;
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
    
    public async Task<bool> Delete(int id)
    {
       return await DeleteUser(id);
    }
}