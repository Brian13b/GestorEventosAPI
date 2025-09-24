using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.DTOs
{
    public class SendMessageDto
    {
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; }

        [Required]
        public int EventId { get; set; }
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string UserRole { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool CanDelete { get; set; } // Si el usuario actual puede eliminar este mensaje
    }

    public class ChatRoomDto
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public int ConnectedUsersCount { get; set; }
        public List<ConnectedUserDto> ConnectedUsers { get; set; } = new();
        public List<ChatMessageDto> RecentMessages { get; set; } = new();
    }

    public class ConnectedUserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public DateTime ConnectedAt { get; set; }
        public bool IsTyping { get; set; }
    }

    public class TypingStatusDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public bool IsTyping { get; set; }
    }
}

