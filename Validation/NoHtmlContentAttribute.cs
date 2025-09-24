using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EventManagementAPI.Validation
{
    public class NoHtmlContentAttribute : ValidationAttribute
    {
        public NoHtmlContentAttribute()
        {
            ErrorMessage = "El contenido no puede contener código HTML";
        }

        public override bool IsValid(object value)
        {
            if (value is not string content)
                return true;

            // Buscar tags HTML básicos
            var htmlPattern = @"<[^>]+>";
            return !Regex.IsMatch(content, htmlPattern, RegexOptions.IgnoreCase);
        }
    }
}

