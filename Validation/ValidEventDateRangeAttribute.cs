using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.Validation
{
    public class ValidEventDateRangeAttribute : ValidationAttribute
    {
        private readonly int _maxMonthsInFuture;

        public ValidEventDateRangeAttribute(int maxMonthsInFuture = 12)
        {
            _maxMonthsInFuture = maxMonthsInFuture;
            ErrorMessage = $"La fecha del evento debe estar entre ahora y {maxMonthsInFuture} meses en el futuro";
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateTime eventDate)
                return true;

            var now = DateTime.UtcNow;
            var maxDate = now.AddMonths(_maxMonthsInFuture);

            return eventDate > now && eventDate <= maxDate;
        }
    }
}

