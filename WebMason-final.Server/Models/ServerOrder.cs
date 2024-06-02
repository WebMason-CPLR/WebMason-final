namespace WebMason_final.Server.Models
{
    public class ServerOrder
    {
        public Guid Id { get; set; } // Clé primaire
        public string ServerType { get; set; }
        public DateTime OrderDate { get; set; }
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}

