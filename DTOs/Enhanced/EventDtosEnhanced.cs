using EventManagementAPI.Validation;
using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.DTOs.Enhanced
{
    public class CreateEventDtoEnhanced
    {
        [Required(ErrorMessage = "El título del evento es obligatorio")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 150 caracteres")]
        [NoHtmlContent]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        [NoHtmlContent]
        public string Description { get; set; }

        [Required(ErrorMessage = "La fecha del evento es obligatoria")]
        [FutureDate]
        [ValidEventDateRange(12)]
        public DateTime Date { get; set; }

        [StringLength(200, ErrorMessage = "La ubicación no puede exceder 200 caracteres")]
        public string Location { get; set; }

        [Range(1, 10000, ErrorMessage = "La capacidad máxima debe estar entre 1 y 10000")]
        public int? MaxCapacity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a 0")]
        public decimal? Price { get; set; }

        public List<string> Tags { get; set; } = new();

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public string Category { get; set; }
    }

    public class UpdateEventDtoEnhanced
    {
        [StringLength(150, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 150 caracteres")]
        [NoHtmlContent]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        [NoHtmlContent]
        public string Description { get; set; }

        [FutureDate]
        [ValidEventDateRange(12)]
        public DateTime? Date { get; set; }

        [StringLength(200, ErrorMessage = "La ubicación no puede exceder 200 caracteres")]
        public string Location { get; set; }

        [Range(1, 10000, ErrorMessage = "La capacidad máxima debe estar entre 1 y 10000")]
        public int? MaxCapacity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a 0")]
        public decimal? Price { get; set; }

        public List<string> Tags { get; set; }

        public string Category { get; set; }
    }

    public class EventDetailDtoEnhanced
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }
        public int? MaxCapacity { get; set; }
        public decimal? Price { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Category { get; set; }
        public int TotalRegistrations { get; set; }
        public bool IsUserRegistered { get; set; }
        public bool IsFull => MaxCapacity.HasValue && TotalRegistrations >= MaxCapacity.Value;
        public List<UserDto> RegisteredUsers { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public UserDto Organizer { get; set; }
    }

    public class EventFilterDto
    {
        public string SearchTerm { get; set; }
        public string Category { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Location { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool? HasAvailableSpots { get; set; }

        [Range(1, 100)]
        public int Page { get; set; } = 1;

        [Range(1, 50)]
        public int PageSize { get; set; } = 10;

        public string SortBy { get; set; } = "Date"; // Date, Title, Price, Registrations
        public bool SortDescending { get; set; } = false;
    }
}