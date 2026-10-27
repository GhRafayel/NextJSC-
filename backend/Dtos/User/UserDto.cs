using Backend.Models;
using System.Text.Json.Serialization;
namespace Backend.Dtos;

public class HistoryDto
{
    public int GamesWon    { get; set; }
    public int GamesLost   { get; set; }
    public int TotalScore  { get; set; }
    public int Level       { get; set; }

    public static HistoryDto From(User u) => new()
    {
        GamesWon   = u.GamesWon,
        GamesLost  = u.GamesLost,
        TotalScore = u.Score,
        Level      = u.Score / 100,
    };
}

public class UserDto
{
    public int      Id          { get; set; }
    public int      Score       { get; set; }
    public string   Email       { get; set; } = "";

    [JsonPropertyName("Username")]
    public string   Username    { get; set; } = "";
    public string   Language    { get; set; } = "en";
    public string?   Color      { get; set; }
    public string   Avatar      { get; set; } = "hero-gray.svg";
    public Role     Role        { get; set; }
    public bool     Theme       { get; set; } = true;
    public DateTime CreatedAt    { get; set; }
    public DateTime? TermsAcceptedAt {get; set;}
    public HistoryDto History   { get; set; } = new();

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
        CreatedAt       = u.CreatedAt,
        TermsAcceptedAt = u.TermsAcceptedAt,
        History         = HistoryDto.From(u),
    };

}