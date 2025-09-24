using EventManagementAPI.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventManagementAPI.Filters
{
    public class ValidateModelFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e => new ValidationErrorResponse
                    {
                        Field = x.Key,
                        Message = e.ErrorMessage,
                        AttemptedValue = x.Value.AttemptedValue
                    }))
                    .ToList();

                var response = new ApiResponse<List<ValidationErrorResponse>>
                {
                    Success = false,
                    Message = "Errores de validación",
                    Data = errors,
                    Timestamp = DateTime.UtcNow
                };

                context.Result = new BadRequestObjectResult(response);
            }
        }
    }
}
