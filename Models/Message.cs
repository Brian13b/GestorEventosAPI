using System;
using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; }
    }
}

