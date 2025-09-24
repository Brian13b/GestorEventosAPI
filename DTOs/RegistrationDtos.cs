namespace EventManagementAPI.DTOs
{
    public class RegistrationDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
