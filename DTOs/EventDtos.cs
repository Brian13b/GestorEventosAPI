using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.DTOs
{
    public class CreateEventDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }

    public class UpdateEventDto
    {
        [StringLength(150)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public DateTime? Date { get; set; }
    }

    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int TotalRegistrations { get; set; }
        public bool IsUserRegistered { get; set; }
    }

    public class EventDetailDto : EventDto
    {
        public List<UserDto> RegisteredUsers { get; set; } = new();
    }

}
