using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsControler : ApiControllerBase
{
    // [HttpGet]
    // public async Task<IActionResult> GetFriends()
    // {
    //     int? id = GetAuthenticatedUserId();
    //     if (id is null)
    //         return Unauthorized();
    //     List<Friends> friend = GetFriends(id);
    //     return NotFound();
    // }
}