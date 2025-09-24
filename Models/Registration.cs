using System;

namespace EventManagementAPI.Models
{
    public class Registration
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }

        public DateTime RegisteredAt { get; set; }
    }
}
