using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend.Services;
public abstract class ApiServiceBase(AppDbContext DB)
{
    public Task<List<User>> GetAllUsers() => DB.Users.ToListAsync();
    public Task<User?> GetUserById(int id) => DB.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetUserByEmail(string email) => DB.Users.FirstOrDefaultAsync(u => u.Email == email);

    protected async Task<User?> Update(int id, Action<User> apply)
    {
        User? user = await GetUserById(id);
        if (user is null) return null;
        apply(user);
        await DB.SaveChangesAsync();
        return user;
    }
}