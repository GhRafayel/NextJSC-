namespace Backend.Models;

public class Session
{
    public int       Id                  { get; set; }
    public int       UserId              { get; set; }
    public string    RefreshToken        { get; set; } = "";
    public User      User                { get; set; } = null!;
    public DateTime  ExpiresAt           { get; set; }
    public DateTime? RevokedAt           { get; set; }    
    public DateTime  CreatedAt           { get; set; } = DateTime.UtcNow;

}