using EventManagementAPI.DTOs;

namespace EventManagementAPI.Services.Interfaces
{
    public interface IEventService
    {
        Task<IEnumerable<EventDto>> GetAllEventsAsync(int userId);
        Task<EventDetailDto> GetEventByIdAsync(int eventId, int userId);
        Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, int organizerId);
        Task<EventDto> UpdateEventAsync(int eventId, UpdateEventDto updateEventDto, int userId);
        Task<bool> DeleteEventAsync(int eventId, int userId);
        Task<bool> RegisterToEventAsync(int eventId, int userId);
        Task<bool> UnregisterFromEventAsync(int eventId, int userId);
        Task<IEnumerable<RegistrationDto>> GetEventRegistrationsAsync(int eventId, int userId);
        Task<bool> AdminUnregisterUserFromEventAsync(int id, int userId, int adminId);
        Task AdminRegisterUserToEventAsync(int id, int userId, int adminId);
        Task GetAvailableUsersForEventAsync(int id, int userId);
    }
}
