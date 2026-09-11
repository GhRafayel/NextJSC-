namespace Backend.Models;

public class User : ITimestamped
{
    public int Id { get; set; }
    public int Score { get; set; } = 0;
    public int ResetCodeAttempts { get; set; } = 0;
    public string   Email {get; set;} = "";
    public string   Username {get; set;} = "";
    public string?  Password {get; set;}
    public string?  NewPassword {get; set; }
    public string?  Provider {get; set;}
    public string?  ProviderId {get; set;}
    public string?  Color {get; set;}
    public string   Language {get; set;} = "en";
    public string   Avatar {get; set;} = "default.png";
    public string?  ResetCode {get; set;}
    public Role     Role {get; set;} = Role.PLAYER;

    public bool     IsBot { get; set;} = false;
    public bool     Theme { get; set; } = true;
    public DateTime? TermsAcceptedAt {get; set;}
    public DateTime? CodeExpire {get; set;}
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;
}