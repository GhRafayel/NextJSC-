namespace Backend.Options;

public class GithubOptions :IOAuthSettings
{
    public string ClientId      { get; set; } = "";
    public string ClientSecret  { get; set; } = "";
    public string CallbackUrl   { get; set; } = "";
} 

public class GoogleOptions : IOAuthSettings
{
    public string ClientId      { get; set; } = "";
    public string ClientSecret  { get; set; } = "";
    public string CallbackUrl   { get; set; } = "";
}

public interface IOAuthSettings
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string CallbackUrl { get; set; }
}