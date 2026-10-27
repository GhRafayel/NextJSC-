using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class UsersController(UserService _users, IWebHostEnvironment env) : ApiControllerBase()
{
    private static readonly HashSet<string> AllowedAvatarExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
    private const long MaxAvatarBytes = 8 * 1024 * 1024;

    [HttpPost("avatar-upload")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });
        if (file.Length > MaxAvatarBytes)
            return BadRequest(new { error = "File too large" });

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedAvatarExtensions.Contains(ext))
            return BadRequest(new { error = "Unsupported file type" });

        User? user = await _users.GetUserById(id.Value);
        if (user is null)
            return NotFound();

        string uploadsDir = Path.Combine(env.ContentRootPath, "UploadedAvatars");
        Directory.CreateDirectory(uploadsDir);

        if (user.Avatar.StartsWith("uploads/avatars/"))
        {
            string oldPath = Path.Combine(uploadsDir, Path.GetFileName(user.Avatar));
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        string fileName = $"{Guid.NewGuid()}{ext}";
        string fullPath = Path.Combine(uploadsDir, fileName);
        using (FileStream stream = new(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        string relativeAvatar = $"uploads/avatars/{fileName}";
        await _users.ChangeAvatar(id.Value, relativeAvatar);

        return Ok(new { avatar = relativeAvatar });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();

        User? user = await _users.GetUserById(id.Value);
        return user is null ? NotFound() : Ok(UserDto.From(user));
    }

    [HttpGet("search/{name}")]
    public async Task<IActionResult> SearchUser(string name)
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        List<User>? users = await _users.Search(id.Value, name);
        return users is null ? NotFound() : Ok(users.Select(UserDto.From));
    }    

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        bool ok = await _users.Delete(id.Value);

        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("accept-terms")]
    public async Task<IActionResult> AcceptTerms()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        User? user = await _users.AcceptTerms(id.Value);
        return user is null ? NotFound() : NoContent();
    }
    
    [HttpPatch("change-username")]
    public async Task<IActionResult> ChangeName([FromBody] ChangeUsernameDto dto)
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        User? user = await _users.ChangeName(id.Value, dto.Username);
        return user is null ? NotFound() : NoContent();
    }

    [HttpPatch("change-avatar")]
    public async Task<IActionResult> ChangeAvatar([FromBody] ChangeAvatarDto dto )
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        User? user = await _users.ChangeAvatar(id.Value, dto.Avatar);
        return user is null ? NotFound() : NoContent();
    }

    [HttpPatch("change-language")]
    public async Task<IActionResult> ChangeLanguage([FromBody] ChangeLanguageDto dto)
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        User? user = await _users.ChangeLanguage(id.Value, dto.Language);
        return user is null ? NotFound() : NoContent();
    }

    [HttpPatch("change-color")]
    public async Task<IActionResult> ChangeColor([FromBody] ChangeColorDto dto)
    {
        int? id = GetAuthenticatedUserId();
        if (id is null) return Unauthorized();

        User? user = await _users.ChangeColor(id.Value, dto.Color);
        return user is null ? NotFound() : NoContent();
    }

    [HttpPatch("change-theme")]
    public async Task<IActionResult> ChangeTheme([FromBody] ChangeThemeDto dto)
    {
        int? id = GetAuthenticatedUserId();
        if (id is null) return Unauthorized();

        User? user = await _users.ChangeTheme(id.Value, dto.Theme);
        return user is null ? NotFound() : NoContent();
    }

    [HttpGet("language/{key}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLanguage(string key)
    {
        Translations? leng = await _users.GetTranslations(key);
        return leng is null ? NotFound() : Content(leng.Values, "application/json");
    }
}
