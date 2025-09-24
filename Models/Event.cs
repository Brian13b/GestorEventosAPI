using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public DateTime Date { get; set; }

        // Navegación
        public ICollection<Registration> Registrations { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}

