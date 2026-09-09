using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class UsersController(UserService _users) : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();

        User? user = await _users.GetUserById(id.Value);
        return user is null ? NotFound() : Ok(UserDto.From(user));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<User> users = await _users.GetAllUsers();
        return Ok(users.Select(UserDto.From));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(int id)
    {
        User? user = await _users.GetUserById(id);
        return user is null ? NotFound() : Ok(UserDto.From(user));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        bool ok =  await  _users.Delete(id.Value);
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
