using System.Text.Json.Serialization;

namespace SASI.Models.Requests
{
    public class SsoTokenRequest
    {
        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = "authorization_code";

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("client_secret")]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("code_verifier")]
        public string? CodeVerifier { get; set; }

        [JsonPropertyName("redirect_uri")]
        public string? RedirectUri { get; set; }
    }
}
