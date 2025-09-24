using EventManagementAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.Validation
{
    public class UniqueEmailAttribute : ValidationAttribute
    {
        public UniqueEmailAttribute()
        {
            ErrorMessage = "Ya existe un usuario con este email";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string email)
                return ValidationResult.Success;

            var context = validationContext.GetService<AppDbContext>();
            if (context == null)
                return ValidationResult.Success;

            var existingUser = context.Users.Any(u => u.Email.ToLower() == email.ToLower());

            return existingUser
                ? new ValidationResult(ErrorMessage)
                : ValidationResult.Success;
        }
    }
}

