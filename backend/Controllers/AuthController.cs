using Microsoft.AspNetCore.Mvc;
using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Backend.Options;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth, IOptions<GoogleOptions> google, IOptions<GithubOptions> github) : ApiControllerBase 
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
        AuthResultDto? res = await auth.Login(dto);
        if (res is null)
            return Unauthorized(new { error = "Wrong email or password"});
        return Ok(res);
    }
    
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback ([FromQuery] string code)
    {
        AuthResultDto? result = await auth.GoogleLogin(code);
        const string Url = "http://localhost:3000/";
        if (result is null)
            return Redirect(Url + "server/login");
        string path = QueryHelpers.AddQueryString(Url + "api/auth",
        new Dictionary<string, string?>
        {
            ["accessToken"] = result.AccessToken,
            ["refreshToken"] = result.RefreshToken,
        });
        return Redirect(path);
    }
    
    [HttpGet("google")]
    public IActionResult Google()
    {
        string url = QueryHelpers.AddQueryString(
        "https://accounts.google.com/o/oauth2/v2/auth",
        new Dictionary<string, string?>
        {
            ["client_id"] = google.Value.ClientId,
            ["redirect_uri"] = google.Value.CallbackUrl,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
        });

        return Redirect(url);
    }
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        var res =  await auth.Refresh(dto);
        if (res is null)
            return Unauthorized(new { error = "Invalid or expired refresh token"});
        return Ok(res);
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordDto dto)
    {
        string? email = await auth.Reset(dto);
        return email is null ? NotFound(new {error = "Email not found"}) : Ok(new { email });
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
    
    [HttpPost("resetCode")]
    public async Task<IActionResult> ResetCode ([FromBody] ResetCodeDto dto)
    {
        bool? result = await auth.ResetCode(dto);

        if (result is null || result == false)
            return BadRequest(new { error = "Code expired or Invalid code" });
        
        return Ok(new { dto.Email});
    }

}