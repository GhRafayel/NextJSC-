using System.ComponentModel.DataAnnotations;
using  Backend.Models;
namespace Backend.Dtos;

public class FriendsDto
{
    public Guid         Id              { get; set; }
    public int          Score           { get; set; }
    public int          RequestId       { get; set; }
    public int          SenderId        { get; set; }
    public int          ReceiverId      { get; set; }
    public string       Username        { get; set; } = "";
    public bool         IsOnline        { get; set; } = false;
    public FriendStatus Status          { get; set; } = FriendStatus.PENDING;
    
    public static FriendsDto From(Friends f, int currentUserId) => new ()
    {
        Id  = f.Id,
        Status = f.Status,
        SenderId = f.SenderId,
        ReceiverId = f.ReceiverId,
        Username = currentUserId == f.SenderId ? f.Receiver.Username : f.Sender.Username,
        Score = currentUserId == f.SenderId ? f.Receiver.Score : f.Sender.Score,
        RequestId = currentUserId == f.SenderId ? f.ReceiverId : f.SenderId,
        IsOnline = f.IsOnline,
    };
}

public class InviteFriendDto
{
    [Required]
    public int ReceiverId { get; set; }
}