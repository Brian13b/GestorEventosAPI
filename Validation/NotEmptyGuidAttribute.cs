using System.ComponentModel.DataAnnotations;

namespace EventManagementAPI.Validation
{
    public class NotEmptyGuidAttribute : ValidationAttribute
    {
        public NotEmptyGuidAttribute()
        {
            ErrorMessage = "El GUID no puede estar vacío";
        }

        public override bool IsValid(object value)
        {
            if (value is Guid guid)
            {
                return guid != Guid.Empty;
            }
            return true;
        }
    }
}
