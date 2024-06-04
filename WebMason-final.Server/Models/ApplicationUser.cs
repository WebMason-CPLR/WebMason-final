using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace WebMason_final.Server.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid Id { get; set; }
        
        [JsonPropertyName("username")]
        public string Username { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public ICollection<ServerOrder> ServerOrders { get; set; }
    }
}

