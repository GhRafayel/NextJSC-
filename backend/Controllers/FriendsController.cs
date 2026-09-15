using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;
using Backend.Dtos;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController(FriendsService Fds, OnlineStateService online): ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFriends ()
    {
        int? id = GetAuthenticatedUserId();
        if (id is null) return Unauthorized();
        List<Friends> friends = await Fds.GetFriends(id.Value);

        foreach (Friends friend in friends)
        {
            int otherUserId = id.Value == friend.SenderId ? friend.ReceiverId : friend.SenderId;
            friend.IsOnline = await online.IsUserOnline(otherUserId);
        }
        return Ok(friends.Select(friend => FriendsDto.From(friend, id.Value)).ToList());
    }

    [HttpDelete("{friendId}")]
    public async Task<IActionResult> DeleteFriend (Guid friendId)
    {
        int? senderId = GetAuthenticatedUserId();
        if (senderId is null)
            return Unauthorized();
        bool result = await Fds.DeleteFriend(senderId.Value, friendId);
        return result ? Ok() : NotFound();
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteFriend ([FromBody] InviteFriendDto dto)
    {
        int? senderId = GetAuthenticatedUserId();
        if (senderId is null)
            return Unauthorized();
        Friends? friend = await Fds.InviteFriend(senderId.Value, dto.ReceiverId);
        return friend is null ? BadRequest("Friend request already exists.") :  Ok(FriendsDto.From(friend, senderId.Value));
    }

    [HttpPatch("accept")]
    public async Task<IActionResult> AcceptFriend([FromBody] InviteFriendDto dto)
    {
        int? senderId = GetAuthenticatedUserId();
        if (senderId is null)
            return Unauthorized();
        Friends? friend = await Fds.AcceptFriend(senderId.Value, dto.ReceiverId);
        return friend is null ? NotFound() :  Ok(FriendsDto.From(friend, senderId.Value));
    }

    [HttpPatch("reject")]
     public async Task<IActionResult> RejectFriend([FromBody] InviteFriendDto dto)
    {
        int? senderId = GetAuthenticatedUserId();
        if (senderId is null)
            return Unauthorized();
        Friends?  friend = await Fds.RejectFriend(senderId.Value, dto.ReceiverId);
        return friend is null ? NotFound() :  Ok(FriendsDto.From(friend, senderId.Value));
    }

    [HttpPatch("cancel")]
    public async Task<IActionResult> CancelInvitation([FromBody] InviteFriendDto dto)
    {
        int? senderId = GetAuthenticatedUserId();
        if (senderId is null)
            return Unauthorized();
        bool ? value = await Fds.CancelInvitation(senderId.Value, dto.ReceiverId);
        return value == true ? Ok() : NotFound();
    }
}