namespace WebMason_final.Server.Models
{
    public class ServerOrder
    {
        public int Id { get; set; } // Clé primaire
        public string ServerType { get; set; }
        public DateTime OrderDate { get; set; }
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}

