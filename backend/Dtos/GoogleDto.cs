using   System.Text.Json.Serialization;

namespace Backend.Dtos;

public class GoogleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = "";

}

public class GoogleUserInfo
{
   [JsonPropertyName("sub")]
   public string Sub { get; set; } = "";

   [JsonPropertyName("email")]
   public string Email {get; set; } = "";

   [JsonPropertyName("name")]
   public string Name { get; set; } = "";

   [JsonPropertyName("picture")]
   public string Picture {get; set; } = "";
}

