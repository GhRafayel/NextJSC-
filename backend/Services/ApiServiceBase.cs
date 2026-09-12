using Backend.Data;
using Backend.Models;
using Backend.Dtos;
using Microsoft.EntityFrameworkCore;
namespace Backend.Services;
public abstract class ApiServiceBase(AppDbContext db,  TokenService tokens)
{
    protected readonly AppDbContext DB = db;
    private readonly TokenService _tokens = tokens;
    public Task<List<User>> GetAllUsers() => DB.Users.ToListAsync();
    public Task<User?> GetUserById(int id) => DB.Users.FirstOrDefaultAsync(u => u.Id == id);
    public Task<User?> GetUserByEmail(string email) => DB.Users.FirstOrDefaultAsync(u => u.Email == email);

    private async Task<User> AddUser (User user)
    {
        DB.Users.Add(user);
        await DB.SaveChangesAsync();
        return user;
    }
    
    protected async Task<bool> DeleteUser(int id)
    {
        int rows = await DB.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
        return rows > 0;
    }
    
    protected async Task<User?> ValidateCredentials(LoginDto dto)
    {
        User?  user = await GetUserByEmail(dto.Email);

        if ( user is null || user.Password is null)
            return null;

        bool ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
        return ok ? user : null;
    }
    
    protected async Task<AuthResultDto> IssueTokens(User user)
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
    
    protected async Task<User?> Create(ExternalUserInfo info)
    {
        if (info.Email is null) return null;
        User? user =  await GetUserByEmail(info.Email);
        if (user is not null)
            return user;
        user = new User
        {
            Email = info.Email,
            Username = info.Name ?? info.Email.Split('@')[0],
            Avatar =  "default.png",
            Provider = info.Provider,
            ProviderId = info.ProviderUserId,
        };
        return await AddUser(user);
    }
    
    protected async Task<User?> Create(CreateUsersDto dto)
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
        return await AddUser(user);
    }
    
    protected async Task<User?> Update(int id, Action<User> apply)
    {
        User? user = await GetUserById(id);
        if (user is null) return null;
        apply(user);
        await DB.SaveChangesAsync();
        return user;
    }
}