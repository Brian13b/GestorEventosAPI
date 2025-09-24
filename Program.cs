using EventManagementAPI.Data;
using EventManagementAPI.Services;
using EventManagementAPI.Services.Interfaces;
using EventManagementAPI.Hubs;
using EventManagementAPI.Middleware;
using EventManagementAPI.Filters;
using EventManagementAPI.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "mi-clave-super-secreta-para-desarrollo-local-2024";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Configuración para SignalR
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // Si la solicitud es para el hub de chat
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configuración de autorización
builder.Services.AddAuthorization();

// Registrar servicios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configuración de cache en memoria para rate limiting
builder.Services.AddMemoryCache();

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// Servicios de controlador con validaciones mejoradas
builder.Services.AddControllers(options =>
{
    // Agregar filtros globales
    options.Filters.Add<ValidateModelFilter>();

    // Configurar JSON options
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "El campo es obligatorio");
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = true;
});

// Agregar servicios de validación personalizados
builder.Services.AddValidationServices();
builder.Services.AddCustomValidation();

// FluentValidation (opcional para validaciones más complejas)
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

// Configuración de Swagger con JWT y documentación mejorada
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventManagement API",
        Version = "v1.0",
        Description = @"
🎪 **API Completa de Gestión de Eventos**

Esta API proporciona funcionalidades completas para:
- 🔐 Autenticación y autorización con JWT
- 🎉 Gestión completa de eventos (CRUD)
- 👥 Sistema de registro a eventos
- 💬 Chat en tiempo real por evento con SignalR
- ⚙️ Panel de administración
- 🛡️ Validaciones robustas y rate limiting

**Roles disponibles:**
- **Usuario**: Registrarse a eventos, participar en chats
- **Organizador**: Crear eventos + permisos de Usuario  
- **Admin**: Gestión completa del sistema

**Instrucciones de uso:**
1. Registrarse o hacer login para obtener JWT token
2. Usar el token en header: `Authorization: Bearer {token}`
3. Crear/unirse a eventos según rol
4. Conectar al chat via SignalR en `/chathub`
        ",
        Contact = new OpenApiContact
        {
            Name = "EventManagement API"
        }
    });

    // Configuración de JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"Autorización JWT usando esquema Bearer.
                      
Ingresa **solo** el token (sin 'Bearer ' al inicio).
                      
Ejemplo: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Configurar respuestas por defecto
    c.DocumentFilter<SwaggerDocumentFilter>();

    // Incluir comentarios XML si existen
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configuración de CORS mejorada
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder
            .WithOrigins(
                "http://localhost:3000",    // React default
                "https://localhost:3000",   // React HTTPS
                "http://localhost:5173",    // Vite default
                "https://localhost:5173",   // Vite HTTPS
                "http://localhost:5135"    // Swagger UI
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configuración de logging mejorado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// En desarrollo, logging más detallado
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Configurar opciones de JSON global
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
    options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


// Middleware pipeline optimizado
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Swagger solo en desarrollo
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventManagement API V1");
        c.RoutePrefix = string.Empty; 
        c.DefaultModelsExpandDepth(-1); 
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.EnableDeepLinking();
        c.EnableFilter();
        c.EnableValidator();

        // Tema personalizado
        c.InjectStylesheet("/swagger-ui/custom.css");
    });

    // Archivos estáticos para la página de prueba
    app.UseStaticFiles();
}
else
{
    // En producción, middleware de manejo de errores más estricto
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Middleware personalizado de validación y manejo de errores
app.UseMiddleware<ValidationMiddleware>();

// CORS
app.UseCors();

// Rate limiting global
if (app.Environment.IsProduction())
{
    app.UseMiddleware<GlobalRateLimitMiddleware>();
}

app.UseAuthentication();
app.UseAuthorization();

// Mapear controladores
app.MapControllers();

// Mapear el Hub de SignalR
app.MapHub<ChatHub>("/chathub");

// Seed inicial de datos con validaciones
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        scopedLogger.LogInformation("🔧 Inicializando base de datos...");

        // Asegurar que la base de datos está creada
        context.Database.EnsureCreated();

        // Seed de roles con validación
        if (!context.Roles.Any())
        {
            var roles = new[]
            {
                new EventManagementAPI.Models.Role { Name = "Admin" },
                new EventManagementAPI.Models.Role { Name = "Organizador" },
                new EventManagementAPI.Models.Role { Name = "Usuario" }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();

            scopedLogger.LogInformation("✅ Roles iniciales creados: Admin, Organizador, Usuario");
        }

        // Crear usuario admin por defecto si no existe
        if (!context.Users.Any(u => u.Email == "admin@test.com"))
        {
            var adminRole = context.Roles.First(r => r.Name == "Admin");
            var adminUser = new EventManagementAPI.Models.User
            {
                Username = "admin",
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                RoleId = adminRole.Id
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            scopedLogger.LogInformation("✅ Usuario admin por defecto creado: admin@test.com / Admin123!");
        }

        // Crear evento de prueba si no existe
        if (!context.Events.Any())
        {
            var testEvent = new EventManagementAPI.Models.Event
            {
                Title = "Evento de Prueba del Sistema",
                Description = "Este es un evento de prueba para demostrar las funcionalidades del chat y registro.",
                Date = DateTime.UtcNow.AddDays(7),
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            scopedLogger.LogInformation("✅ Evento de prueba creado");
        }

        scopedLogger.LogInformation("🚀 Inicialización de datos completada");
    }
    catch (Exception ex)
    {
        var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        scopedLogger.LogError(ex, "❌ Error durante la inicialización de la base de datos");
        throw;
    }
}

// Mensaje de bienvenida en consola
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🎪 EventManagement API iniciada correctamente!");
logger.LogInformation("📊 Swagger UI disponible en: https://localhost:5135");
logger.LogInformation("🧪 Página de pruebas en: https://localhost:5135/chat-test.html");
logger.LogInformation("💬 SignalR Hub en: https://localhost:5135/chathub");
logger.LogInformation("❤️  Health checks en: https://localhost:5135/health");

if (app.Environment.IsDevelopment())
{
    logger.LogInformation("🔓 Usuario admin por defecto: admin@test.com / Admin123!");
}

app.Run();

// Clase auxiliar para filtro de documentación Swagger
public class SwaggerDocumentFilter : Swashbuckle.AspNetCore.SwaggerGen.IDocumentFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiDocument swaggerDoc, Swashbuckle.AspNetCore.SwaggerGen.DocumentFilterContext context)
    {
        // Agregar información adicional al documento Swagger
        swaggerDoc.Info.Extensions.Add("x-logo", new Microsoft.OpenApi.Any.OpenApiObject
        {
            ["url"] = new Microsoft.OpenApi.Any.OpenApiString("https://via.placeholder.com/120x60/667eea/ffffff?text=EventAPI")
        });
    }
}

// Middleware adicional para rate limiting global (opcional)
public class GlobalRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalRateLimitMiddleware> _logger;

    public GlobalRateLimitMiddleware(RequestDelegate next, ILogger<GlobalRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }
}