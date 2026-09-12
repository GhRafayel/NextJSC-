using   System.Text.Json.Serialization;
using   System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

public class GitHubUserInfoDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("login")]
    public string Login { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}

public class GoogleUserInfoDto
{
    [JsonPropertyName("sub")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

public class ExternalUserInfo
{
    public string Provider { get; set; } = null!;
    public string ProviderUserId { get; set; } = null!;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
}

public class GitHubEmailDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }
}

public class LoginDto
{
    [Required] [EmailAddress]
    public string Email {get; set;} = "";

    [Required]
    public string Password {get; set;} = "";
}
