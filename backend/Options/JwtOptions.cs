namespace Backend.Options;

public class JwtOptions
{
    public string   Key                 { get; set; } = "";
    public string   Issuer              { get; set; } = "";
    public string   Audience            { get; set; } = "";
    public int      AccessTokenMinutes  { get; set; }
    public int      RefreshTokenDays    { get; set; }
}