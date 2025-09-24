using EventManagementAPI.Data;
using EventManagementAPI.DTOs;
using EventManagementAPI.Models;
using EventManagementAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventManagementAPI.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChatService> _logger;

        // Cache para conexiones activas
        private static readonly Dictionary<string, ChatConnection> _connections = new();
        private static readonly Dictionary<int, Dictionary<int, DateTime>> _userMessageTimes = new();

        private const int RATE_LIMIT_MESSAGES = 10; // Mensajes por minuto
        private const int RATE_LIMIT_WINDOW_MINUTES = 1;
        private const int MESSAGE_HISTORY_PAGE_SIZE = 50;

        public ChatService(AppDbContext context, IMemoryCache cache, ILogger<ChatService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> CanUserJoinChatAsync(int eventId, int userId)
        {
            // Verificar que el evento existe
            var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
            if (!eventExists)
                return false;

            // Verificar que el usuario está registrado al evento
            var isRegistered = await _context.Registrations
                .AnyAsync(r => r.EventId == eventId && r.UserId == userId);

            return isRegistered;
        }

        public async Task<ChatRoomDto> JoinChatRoomAsync(int eventId, int userId)
        {
            if (!await CanUserJoinChatAsync(eventId, userId))
                throw new UnauthorizedAccessException("No tienes acceso a este chat. Debes estar registrado al evento.");

            var eventEntity = await _context.Events.FindAsync(eventId);
            var connectedUsers = await GetConnectedUsersAsync(eventId);
            var recentMessages = await GetMessageHistoryAsync(eventId, userId, 1, 20);

            return new ChatRoomDto
            {
                EventId = eventId,
                EventTitle = eventEntity.Title,
                ConnectedUsersCount = connectedUsers.Count,
                ConnectedUsers = connectedUsers,
                RecentMessages = recentMessages
            };
        }

        public async Task<ChatMessageDto> SendMessageAsync(SendMessageDto sendMessageDto, int userId)
        {
            // Verificar permisos
            if (!await CanUserJoinChatAsync(sendMessageDto.EventId, userId))
                throw new UnauthorizedAccessException("No puedes enviar mensajes a este chat");

            // Verificar rate limiting
            if (!await CheckRateLimitAsync(userId, sendMessageDto.EventId))
                throw new InvalidOperationException($"Límite de mensajes excedido. Máximo {RATE_LIMIT_MESSAGES} mensajes por minuto.");

            // Obtener información del usuario
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstAsync(u => u.Id == userId);

            // Crear y guardar mensaje
            var message = new Message
            {
                EventId = sendMessageDto.EventId,
                UserId = userId,
                Content = sendMessageDto.Content.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Registrar tiempo del mensaje para rate limiting
            RecordMessageTime(userId, sendMessageDto.EventId);

            _logger.LogInformation($"Usuario {user.Username} envió mensaje en evento {sendMessageDto.EventId}");

            return new ChatMessageDto
            {
                Id = message.Id,
                EventId = message.EventId,
                UserId = message.UserId,
                Username = user.Username,
                UserRole = user.Role.Name,
                Content = message.Content,
                SentAt = message.SentAt,
                CanDelete = CanUserDeleteMessage(userId, message.UserId, user.Role.Name)
            };
        }

        public async Task<List<ChatMessageDto>> GetMessageHistoryAsync(int eventId, int userId, int page = 1, int pageSize = 50)
        {
            if (!await CanUserJoinChatAsync(eventId, userId))
                throw new UnauthorizedAccessException("No tienes acceso a este historial de chat");

            // Obtener rol del usuario actual
            var currentUser = await _context.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId);

            var messages = await _context.Messages
                .Where(m => m.EventId == eventId)
                .Include(m => m.User)
                    .ThenInclude(u => u.Role)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            var messageDtos = messages
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    EventId = m.EventId,
                    UserId = m.UserId,
                    Username = m.User.Username,
                    UserRole = m.User.Role.Name,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    CanDelete = CanUserDeleteMessage(userId, m.UserId, currentUser.Role.Name)
                })
                .OrderBy(m => m.SentAt)
                .ToList();

            return messageDtos;
        }

        public async Task<bool> DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _context.Messages
                .Include(m => m.User)
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
                return false;

            // Verificar permisos
            var currentUser = await _context.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId);

            if (!CanUserDeleteMessage(userId, message.UserId, currentUser.Role.Name))
                throw new UnauthorizedAccessException("No tienes permisos para eliminar este mensaje");

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Mensaje {messageId} eliminado por usuario {userId}");
            return true;
        }

        public async Task AddConnectionAsync(string connectionId, int userId, int eventId)
        {
            var connection = new ChatConnection
            {
                ConnectionId = connectionId,
                UserId = userId,
                EventId = eventId,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _connections[connectionId] = connection;
            _logger.LogInformation($"Usuario {userId} conectado al chat del evento {eventId}");
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            if (_connections.Remove(connectionId, out var connection))
            {
                _logger.LogInformation($"Usuario {connection.UserId} desconectado del chat del evento {connection.EventId}");
            }
        }

        public async Task<List<ConnectedUserDto>> GetConnectedUsersAsync(int eventId)
        {
            var connectedUsers = _connections.Values
                .Where(c => c.EventId == eventId)
                .GroupBy(c => c.UserId)
                .Select(g => g.First())
                .ToList();

            var userIds = connectedUsers.Select(c => c.UserId).ToList();

            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            return connectedUsers.Select(c =>
            {
                var user = users.First(u => u.Id == c.UserId);
                return new ConnectedUserDto
                {
                    UserId = c.UserId,
                    Username = user.Username,
                    Role = user.Role.Name,
                    ConnectedAt = c.ConnectedAt,
                    IsTyping = false
                };
            }).OrderBy(u => u.Username).ToList();
        }

        public async Task UpdateLastActivityAsync(string connectionId)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                connection.LastActivity = DateTime.UtcNow;
            }
        }

        public async Task<bool> CheckRateLimitAsync(int userId, int eventId)
        {
            var cacheKey = $"rate_limit_{userId}_{eventId}";

            if (!_userMessageTimes.ContainsKey(userId))
                _userMessageTimes[userId] = new Dictionary<int, DateTime>();

            if (!_userMessageTimes[userId].ContainsKey(eventId))
                _userMessageTimes[userId][eventId] = DateTime.UtcNow.AddMinutes(-RATE_LIMIT_WINDOW_MINUTES - 1);

            var lastMessages = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();
            var cutoffTime = DateTime.UtcNow.AddMinutes(-RATE_LIMIT_WINDOW_MINUTES);

            // Limpiar mensajes antiguos
            lastMessages = lastMessages.Where(time => time > cutoffTime).ToList();

            if (lastMessages.Count >= RATE_LIMIT_MESSAGES)
                return false;

            return true;
        }

        private void RecordMessageTime(int userId, int eventId)
        {
            var cacheKey = $"rate_limit_{userId}_{eventId}";
            var lastMessages = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();
            var cutoffTime = DateTime.UtcNow.AddMinutes(-RATE_LIMIT_WINDOW_MINUTES);

            // Limpiar mensajes antiguos y agregar el nuevo
            lastMessages = lastMessages.Where(time => time > cutoffTime).ToList();
            lastMessages.Add(DateTime.UtcNow);

            _cache.Set(cacheKey, lastMessages, TimeSpan.FromMinutes(RATE_LIMIT_WINDOW_MINUTES));
        }

        private bool CanUserDeleteMessage(int currentUserId, int messageUserId, string currentUserRole)
        {
            // El usuario puede eliminar sus propios mensajes
            if (currentUserId == messageUserId)
                return true;

            // Admins pueden eliminar cualquier mensaje
            if (currentUserRole == "Admin")
                return true;

            return false;
        }
    }
}
