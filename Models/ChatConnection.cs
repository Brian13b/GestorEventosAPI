namespace EventManagementAPI.Models
{
    public class ChatConnection
    {
        public string ConnectionId { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivity { get; set; }
    }
}

