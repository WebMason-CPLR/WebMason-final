using System.Text.Json.Serialization;

namespace WebMason_final.Server.Models
{
    public class LoginModel
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }
        
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}

