using EventManagementAPI.DTOs;

namespace EventManagementAPI.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatRoomDto> JoinChatRoomAsync(int eventId, int userId);
        Task<ChatMessageDto> SendMessageAsync(SendMessageDto sendMessageDto, int userId);
        Task<List<ChatMessageDto>> GetMessageHistoryAsync(int eventId, int userId, int page = 1, int pageSize = 50);
        Task<bool> DeleteMessageAsync(int messageId, int userId);
        Task<bool> CanUserJoinChatAsync(int eventId, int userId);
        Task AddConnectionAsync(string connectionId, int userId, int eventId);
        Task RemoveConnectionAsync(string connectionId);
        Task<List<ConnectedUserDto>> GetConnectedUsersAsync(int eventId);
        Task UpdateLastActivityAsync(string connectionId);
        Task<bool> CheckRateLimitAsync(int userId, int eventId);
    }
}
