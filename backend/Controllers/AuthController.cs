using Microsoft.AspNetCore.Mvc;
using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ApiControllerBase 
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUsersDto dto)
    {
        AuthResultDto? res = await auth.Register(dto);
        if (res is null)
           return Conflict(new { error = "Registration failed" });
        return Ok(res);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var res = await auth.Login(dto);
        if (res is null)
            return Unauthorized(new { error = "Wrong email or password"});
        return Ok(res);
    }
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        var res =  await auth.Refresh(dto);
        if (res is null)
            return Unauthorized(new { error = "Invalid or expired refresh token"});
        return Ok(res);
    }

    [HttpDelete("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        await auth.Logout(id.Value);
        return NoContent();
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount ()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null)
            return Unauthorized();
        bool val = await auth.DeleteAccount(id.Value);
        return val ? NoContent() : NotFound();
    }
}