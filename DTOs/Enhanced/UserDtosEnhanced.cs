using EventManagementAPI.Validation;
using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.DTOs.Enhanced
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalEventsRegistered { get; set; }
        public int TotalEventsCreated { get; set; }
        public bool IsEmailVerified { get; set; }
    }

    public class UpdateUserProfileDto
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 100 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "El nombre de usuario solo puede contener letras, números, guiones y guiones bajos")]
        public string Username { get; set; }

        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "El nombre completo no puede exceder 100 caracteres")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string PhoneNumber { get; set; }
    }

    public class AdminCreateUserDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        [UniqueEmail]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StrongPassword]
        public string Password { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [RegularExpression(@"^(Admin|Organizador|Usuario)$", ErrorMessage = "Rol inválido")]
        public string Role { get; set; }

        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
    }
}
