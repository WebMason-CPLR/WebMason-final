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

        [JsonPropertyName("mysqlport")]
        public int MySQLPort { get; set; } // Port MySQL

        [JsonPropertyName("wordpressport")]
        public int WordPressPort { get; set; } // Port WordPress

        [JsonPropertyName("odoocontainerid")]
        public string OdooContainerId { get; set; } // ID du conteneur Odoo

        [JsonPropertyName("odoopostgresqlcontainerid")]
        public string OdooPostgreSQLContainerId { get; set; } // ID du conteneur PostgreSQL pour Odoo

        [JsonPropertyName("odoopostgresqlport")]
        public int OdooPostgreSQLPort { get; set; } // Port PostgreSQL pour Odoo

        [JsonPropertyName("odooport")]
        public int OdooPort { get; set; } // Port Odoo

        [JsonPropertyName("redminecontainerid")]
        public string RedmineContainerId { get; set; } // ID du conteneur Redmine

        [JsonPropertyName("redminemysqlcontainerid")]
        public string RedmineMySQLContainerId { get; set; } // ID du conteneur MySQL pour Redmine

        [JsonPropertyName("redminemysqlport")]
        public int RedmineMySQLPort { get; set; } // Port MySQL pour Redmine

        [JsonPropertyName("redmineport")]
        public int RedminePort { get; set; } // Port Redmine
    }
}
