using EventManagementAPI.Filters;
using EventManagementAPI.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using FluentValidation.AspNetCore;

namespace EventManagementAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddValidationServices(this IServiceCollection services)
        {
            // Configurar comportamiento de validación automática
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true; // Manejamos nosotros las validaciones
            });

            // Registrar filtros globales
            services.AddScoped<ValidateModelFilter>();
            services.AddScoped<RateLimitFilter>();

            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationClientsideAdapters();
            // Validadores FluentValidation (opcional)
            services.AddValidatorsFromAssemblyContaining<Program>();

            return services;
        }

        public static IServiceCollection AddCustomValidation(this IServiceCollection services)
        {
            // Registrar validadores personalizados si es necesario
            services.AddTransient<FutureDateAttribute>();
            services.AddTransient<StrongPasswordAttribute>();
            services.AddTransient<UniqueEmailAttribute>();
            services.AddTransient<ValidEventDateRangeAttribute>();
            services.AddTransient<NoHtmlContentAttribute>();

            return services;
        }
    }
}
