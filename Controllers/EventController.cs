using EventManagementAPI.DTOs.Enhanced;
using EventManagementAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        // Obtener todos los eventos con filtros opcionales
        [HttpGet]
        public async Task<IActionResult> GetAllEvents([FromQuery] EventFilterDto filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var events = await _eventService.GetAllEventsAsync(userId);
                return Ok(new { message = "Eventos obtenidos exitosamente", data = events });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Obtener evento especifico por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var eventDetail = await _eventService.GetEventByIdAsync(id, userId);
                return Ok(new { message = "Evento obtenido exitosamente", data = eventDetail });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Crear un nuevo evento
        [HttpPost]
        [Authorize(Roles = "Admin,Organizador")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDtoEnhanced createEventDto)
        {
            try
            {
                var organizerId = GetCurrentUserId();

                var basicDto = new DTOs.CreateEventDto
                {
                    Title = createEventDto.Title,
                    Description = createEventDto.Description,
                    Date = createEventDto.Date
                };

                var eventDto = await _eventService.CreateEventAsync(basicDto, organizerId);
                return CreatedAtAction(nameof(GetEventById), new { id = eventDto.Id },
                    new { message = "Evento creado exitosamente", data = eventDto });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Actualizar un evento existente
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDtoEnhanced updateEventDto)
        {
            try
            {
                var userId = GetCurrentUserId();

                var basicDto = new DTOs.UpdateEventDto
                {
                    Title = updateEventDto.Title,
                    Description = updateEventDto.Description,
                    Date = updateEventDto.Date
                };

                var eventDto = await _eventService.UpdateEventAsync(id, basicDto, userId);
                return Ok(new { message = "Evento actualizado exitosamente", data = eventDto });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Eliminar un evento
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var deleted = await _eventService.DeleteEventAsync(id, userId);

                if (!deleted)
                    return NotFound(new { message = "Evento no encontrado" });

                return Ok(new { message = "Evento eliminado exitosamente" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Registrar al usuario actual en un evento
        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterToEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _eventService.RegisterToEventAsync(id, userId);
                return Ok(new { message = "Registrado exitosamente al evento" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Cancelar registro del usuario actual en un evento
        [HttpDelete("{id}/register")]
        public async Task<IActionResult> UnregisterFromEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var unregistered = await _eventService.UnregisterFromEventAsync(id, userId);

                if (!unregistered)
                    return BadRequest(new { message = "No estás registrado en este evento" });

                return Ok(new { message = "Registro cancelado exitosamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Obtener todos los registros para un evento (solo para Admin y Organizador)
        [HttpGet("{id}/registrations")]
        [Authorize(Roles = "Admin,Organizador")]
        public async Task<IActionResult> GetEventRegistrations(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var registrations = await _eventService.GetEventRegistrationsAsync(id, userId);
                return Ok(new { message = "Registros obtenidos exitosamente", data = registrations });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Otros endpoints relacionados con eventos (Solo Admin y Organizador)

        // Obtener usuarios registrados para un evento
        [HttpGet("{id}/available-users")]
        [Authorize(Roles = "Admin,Organizador")]
        public async Task<IActionResult> GetAvailableUsersForEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _eventService.GetAvailableUsersForEventAsync(id, userId);
                return Ok(new { message = "Usuarios obtenidos exitosamente" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Agregar usuario a un evento
        [HttpPost("{id}/admin-register/{userId}")]
        [Authorize(Roles = "Admin,Organizador")]
        public async Task<IActionResult> AdminRegisterUserToEvent(int id, int userId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                await _eventService.AdminRegisterUserToEventAsync(id, userId, adminId);
                return Ok(new { message = "Usuario registrado exitosamente al evento" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // Remover usuario de un evento
        [HttpDelete("{id}/admin-register/{userId}")]
        [Authorize(Roles = "Admin,Organizador")]
        public async Task<IActionResult> AdminUnregisterUserFromEvent(int id, int userId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var unregistered = await _eventService.AdminUnregisterUserFromEventAsync(id, userId, adminId);
                if (!unregistered)
                    return BadRequest(new { message = "El usuario no está registrado en este evento" });
                return Ok(new { message = "Usuario removido exitosamente del evento" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }
    }
}
