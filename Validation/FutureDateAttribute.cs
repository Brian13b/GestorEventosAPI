using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.Validation
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            ErrorMessage = "La fecha debe ser futura";
        }

        public override bool IsValid(object? value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime > DateTime.UtcNow;
            }

            if (value is DateOnly dateOnly)
            {
                return dateOnly > DateOnly.FromDateTime(DateTime.UtcNow);
            }

            return true; // Si no es DateTime, deja que otras validaciones se encarguen
        }
    }
}
