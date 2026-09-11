namespace Backend.Models;

public class Friends : ITimestamped
{
    public Guid Id              { get; set; }
    public int  SenderId        { get; set; }
    public User Sender          { get; set; } = null!;
    public int  ReceiverId      { get; set; }
    public User Receiver        { get; set; } = null!;
    public FriendStatus Status { get; set; } = FriendStatus.PENDING;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;
}