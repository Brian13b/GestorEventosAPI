using EventManagementAPI.Validation;
using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.DTOs.Enhanced
{
    public class SendMessageDtoEnhanced
    {
        [Required(ErrorMessage = "El contenido del mensaje es obligatorio")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "El mensaje debe tener entre 1 y 1000 caracteres")]
        [NoHtmlContent]
        public string Content { get; set; }

        [Required(ErrorMessage = "El ID del evento es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID de evento inválido")]
        public int EventId { get; set; }

        public string MessageType { get; set; } = "text"; // text, image, file
    }

    public class ChatMessageDtoEnhanced
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string UserRole { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; }
        public DateTime SentAt { get; set; }
        public bool CanDelete { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class EditMessageDto
    {
        [Required(ErrorMessage = "El contenido del mensaje es obligatorio")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "El mensaje debe tener entre 1 y 1000 caracteres")]
        [NoHtmlContent]
        public string Content { get; set; }
    }
}