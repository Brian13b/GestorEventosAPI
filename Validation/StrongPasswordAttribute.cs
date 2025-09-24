using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EventManagementAPI.Validation
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public StrongPasswordAttribute()
        {
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres, incluir mayúsculas, minúsculas y números";
        }

        public override bool IsValid(object value)
        {
            if (value is not string password)
                return false;

            if (password.Length < 8)
                return false;

            // Al menos una mayúscula, una minúscula, un número
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);

            return hasUpper && hasLower && hasDigit;
        }
    }
}
