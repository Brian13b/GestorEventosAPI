using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace EventManagementAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; }

        [Required, StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        // Navegación
        public ICollection<Registration> Registrations { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}

