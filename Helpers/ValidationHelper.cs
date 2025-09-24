using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EventManagementAPI.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailAttribute = new EmailAddressAttribute();
            return emailAttribute.IsValid(email);
        }

        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch));

            return hasUpper && hasLower && hasDigit;
        }

        public static bool ContainsHtml(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            var htmlPattern = @"<[^>]+>";
            return Regex.IsMatch(content, htmlPattern, RegexOptions.IgnoreCase);
        }

        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Remover HTML tags básicos
            var htmlPattern = @"<[^>]+>";
            var sanitized = Regex.Replace(input, htmlPattern, string.Empty);

            // Remover caracteres peligrosos
            sanitized = sanitized.Replace("<script", "")
                                .Replace("javascript:", "")
                                .Replace("vbscript:", "")
                                .Replace("onload=", "");

            return sanitized.Trim();
        }

        public static List<ValidationResult> ValidateObject(object obj)
        {
            var context = new ValidationContext(obj);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

            return results;
        }

        public static bool IsValidEventDateRange(DateTime eventDate, int maxMonthsInFuture = 12)
        {
            var now = DateTime.UtcNow;
            var maxDate = now.AddMonths(maxMonthsInFuture);

            return eventDate > now && eventDate <= maxDate;
        }

        public static bool IsValidCapacity(int? capacity)
        {
            return !capacity.HasValue || (capacity.Value > 0 && capacity.Value <= 10000);
        }

        public static bool IsValidPrice(decimal? price)
        {
            return !price.HasValue || price.Value >= 0;
        }
    }
}
