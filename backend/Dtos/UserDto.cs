using Backend.Models;
using System.Text.Json.Serialization;
namespace Backend.Dtos;

public class UserDto
{
    public int      Id          { get; set; }
    public int      Score       { get; set; }
    public string   Email       { get; set; } = "";

    [JsonPropertyName("Username")]
    public string   Username    { get; set; } = "";
    public string   Language    { get; set; } = "en";
    public string?   Color      { get; set; }
    public string   Avatar      { get; set; } = "default.png";
    public Role     Role        { get; set; }
    public bool     Theme       { get; set; } = true;
    public bool     IsBot       { get; set; }
    public DateTime CreatedAt    { get; set; }
    public DateTime? TermsAcceptedAt {get; set;}

    public static UserDto From(User u) => new()
    {
        Id              = u.Id,
        Score           = u.Score,
        Email           = u.Email,
        Username        = u.Username,
        Language        = u.Language,
        Theme           = u.Theme,
        Color           = u.Color,
        Avatar          = u.Avatar,
        Role            = u.Role,
        IsBot           = u.IsBot,
        CreatedAt       = u.CreatedAt,
        TermsAcceptedAt = u.TermsAcceptedAt,
    };

}