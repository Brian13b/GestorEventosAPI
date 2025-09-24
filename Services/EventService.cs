using EventManagementAPI.Data;
using EventManagementAPI.DTOs;
using EventManagementAPI.Models;
using EventManagementAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagementAPI.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;

        public EventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EventDto>> GetAllEventsAsync(int userId)
        {
            var events = await _context.Events
                .Include(e => e.Registrations)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    Date = e.Date,
                    TotalRegistrations = e.Registrations.Count,
                    IsUserRegistered = e.Registrations.Any(r => r.UserId == userId)
                })
                .OrderBy(e => e.Date)
                .ToListAsync();

            return events;
        }

        public async Task<EventDetailDto> GetEventByIdAsync(int eventId, int userId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.User)
                        .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
                throw new KeyNotFoundException("Evento no encontrado");

            return new EventDetailDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Date = eventEntity.Date,
                TotalRegistrations = eventEntity.Registrations.Count,
                IsUserRegistered = eventEntity.Registrations.Any(r => r.UserId == userId),
                RegisteredUsers = eventEntity.Registrations.Select(r => new UserDto
                {
                    Id = r.User.Id,
                    Username = r.User.Username,
                    Email = r.User.Email,
                    Role = r.User.Role.Name
                }).ToList()
            };
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, int organizerId)
        {
            // Verificar que el usuario es organizador o admin
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == organizerId);

            if (user == null)
                throw new UnauthorizedAccessException("Usuario no encontrado");

            if (user.Role.Name != "Organizador" && user.Role.Name != "Admin")
                throw new UnauthorizedAccessException("Solo organizadores y administradores pueden crear eventos");

            // Validar fecha del evento
            if (createEventDto.Date <= DateTime.UtcNow)
                throw new InvalidOperationException("La fecha del evento debe ser futura");

            var eventEntity = new Event
            {
                Title = createEventDto.Title,
                Description = createEventDto.Description,
                Date = createEventDto.Date
            };

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            return new EventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Date = eventEntity.Date,
                TotalRegistrations = 0,
                IsUserRegistered = false
            };
        }

        public async Task<EventDto> UpdateEventAsync(int eventId, UpdateEventDto updateEventDto, int userId)
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
                throw new KeyNotFoundException("Evento no encontrado");

            // Verificar permisos (solo admin puede editar cualquier evento)
            var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId);
            if (user.Role.Name != "Admin")
                throw new UnauthorizedAccessException("Solo administradores pueden editar eventos");

            // Actualizar campos si se proporcionan
            if (!string.IsNullOrWhiteSpace(updateEventDto.Title))
                eventEntity.Title = updateEventDto.Title;

            if (updateEventDto.Description != null)
                eventEntity.Description = updateEventDto.Description;

            if (updateEventDto.Date.HasValue)
            {
                if (updateEventDto.Date <= DateTime.UtcNow)
                    throw new InvalidOperationException("La fecha del evento debe ser futura");
                eventEntity.Date = updateEventDto.Date.Value;
            }

            await _context.SaveChangesAsync();

            var registrationCount = await _context.Registrations.CountAsync(r => r.EventId == eventId);
            var isUserRegistered = await _context.Registrations.AnyAsync(r => r.EventId == eventId && r.UserId == userId);

            return new EventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Date = eventEntity.Date,
                TotalRegistrations = registrationCount,
                IsUserRegistered = isUserRegistered
            };
        }

        public async Task<bool> DeleteEventAsync(int eventId, int userId)
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
                return false;

            // Verificar permisos (solo admin puede eliminar eventos)
            var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId);
            if (user.Role.Name != "Admin")
                throw new UnauthorizedAccessException("Solo administradores pueden eliminar eventos");

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RegisterToEventAsync(int eventId, int userId)
        {
            // Verificar que el evento existe
            var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
            if (!eventExists)
                throw new KeyNotFoundException("Evento no encontrado");

            // Verificar que el evento no haya pasado
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity.Date <= DateTime.UtcNow)
                throw new InvalidOperationException("No se puede registrar a un evento que ya pasó");

            // Verificar si ya está registrado
            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (existingRegistration != null)
                throw new InvalidOperationException("Ya estás registrado en este evento");

            // Crear registro
            var registration = new Registration
            {
                EventId = eventId,
                UserId = userId,
                RegisteredAt = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnregisterFromEventAsync(int eventId, int userId)
        {
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (registration == null)
                return false;

            // Verificar que el evento no haya pasado
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity.Date <= DateTime.UtcNow)
                throw new InvalidOperationException("No se puede cancelar registro de un evento que ya pasó");

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RegistrationDto>> GetEventRegistrationsAsync(int eventId, int userId)
        {
            // Verificar permisos (solo admin puede ver todas las inscripciones)
            var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId);
            if (user.Role.Name != "Admin")
                throw new UnauthorizedAccessException("Solo administradores pueden ver las inscripciones");

            var registrations = await _context.Registrations
                .Where(r => r.EventId == eventId)
                .Include(r => r.User)
                .Include(r => r.Event)
                .Select(r => new RegistrationDto
                {
                    UserId = r.UserId,
                    Username = r.User.Username,
                    Email = r.User.Email,
                    EventId = r.EventId,
                    EventTitle = r.Event.Title,
                    RegisteredAt = r.RegisteredAt
                })
                .OrderBy(r => r.RegisteredAt)
                .ToListAsync();

            return registrations;
        }
    }
}
