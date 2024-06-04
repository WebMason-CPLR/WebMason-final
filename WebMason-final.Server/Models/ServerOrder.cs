using System.Text.Json.Serialization;

namespace WebMason_final.Server.Models
{
    public class ServerOrder
    {
        public Guid Id { get; set; } // Clé primaire
        [JsonPropertyName("serverType")]
        public string ServerType { get; set; }

        [JsonPropertyName("orderdate")]
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public ApplicationUser User { get; set; }

        [JsonPropertyName("mysqlcontainerid")]
        public string MySQLContainerId { get; set; } // ID du conteneur MySQL

        [JsonPropertyName("wordpresscontainerid")]
        public string WordPressContainerId { get; set; } // ID du conteneur WordPress
    }
}


