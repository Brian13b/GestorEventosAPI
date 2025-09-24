using EventManagementAPI.DTOs;
using EventManagementAPI.DTOs.Enhanced;
using EventManagementAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventManagementAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        // Diccionario para trackear usuarios escribiendo
        private static readonly Dictionary<string, Dictionary<int, string>> _typingUsers = new();

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task JoinEventChat(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Verificar permisos
                if (!await _chatService.CanUserJoinChatAsync(eventId, userId))
                {
                    await Clients.Caller.SendAsync("Error", "No tienes acceso a este chat. Debes estar registrado al evento.");
                    return;
                }

                var groupName = GetEventGroupName(eventId);

                // Unirse al grupo del evento
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                // Registrar conexión
                await _chatService.AddConnectionAsync(Context.ConnectionId, userId, eventId);

                // Obtener datos de la sala
                var chatRoom = await _chatService.JoinChatRoomAsync(eventId, userId);

                // Notificar al usuario que se unió exitosamente
                await Clients.Caller.SendAsync("JoinedChat", chatRoom);

                // Notificar a otros usuarios que alguien se unió
                var username = GetCurrentUsername();
                await Clients.Group(groupName).SendAsync("UserJoined", new
                {
                    UserId = userId,
                    Username = username,
                    Message = $"{username} se unió al chat"
                });

                // Actualizar lista de usuarios conectados
                var connectedUsers = await _chatService.GetConnectedUsersAsync(eventId);
                await Clients.Group(groupName).SendAsync("ConnectedUsersUpdated", connectedUsers);

                _logger.LogInformation($"Usuario {username} ({userId}) se unió al chat del evento {eventId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al unirse al chat del evento {eventId}");
                await Clients.Caller.SendAsync("Error", "Error al unirse al chat");
            }
        }

        public async Task LeaveEventChat(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();
                var groupName = GetEventGroupName(eventId);

                // Salir del grupo
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                // Remover de usuarios escribiendo si estaba escribiendo
                StopTyping(eventId);

                // Notificar a otros usuarios
                await Clients.Group(groupName).SendAsync("UserLeft", new
                {
                    UserId = userId,
                    Username = username,
                    Message = $"{username} salió del chat"
                });

                _logger.LogInformation($"Usuario {username} ({userId}) salió del chat del evento {eventId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al salir del chat del evento {eventId}");
            }
        }

        public async Task SendMessage(SendMessageDtoEnhanced messageDto)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Mapear Enhanced DTO al DTO básico del servicio
                var basicDto = new DTOs.SendMessageDto
                {
                    EventId = messageDto.EventId,
                    Content = messageDto.Content
                };

                var chatMessage = await _chatService.SendMessageAsync(basicDto, userId);

                // Resto del código igual...
                await StopTyping(messageDto.EventId);

                var groupName = GetEventGroupName(messageDto.EventId);
                await Clients.Group(groupName).SendAsync("MessageReceived", chatMessage);

                _logger.LogInformation($"Mensaje enviado por {chatMessage.Username} en evento {messageDto.EventId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje");
                await Clients.Caller.SendAsync("Error", "Error al enviar el mensaje");
            }
        }

        public async Task DeleteMessage(int messageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _chatService.DeleteMessageAsync(messageId, userId);

                if (success)
                {
                    // Notificar a todos los usuarios en los grupos relevantes que el mensaje fue eliminado
                    // Necesitamos encontrar el evento del mensaje para notificar al grupo correcto
                    await Clients.All.SendAsync("MessageDeleted", messageId);
                    _logger.LogInformation($"Mensaje {messageId} eliminado por usuario {userId}");
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "No se pudo eliminar el mensaje");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar mensaje {messageId}");
                await Clients.Caller.SendAsync("Error", "Error al eliminar el mensaje");
            }
        }

        public async Task StartTyping(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();
                var groupName = GetEventGroupName(eventId);

                // Verificar permisos
                if (!await _chatService.CanUserJoinChatAsync(eventId, userId))
                    return;

                // Agregar a la lista de usuarios escribiendo
                if (!_typingUsers.ContainsKey(groupName))
                    _typingUsers[groupName] = new Dictionary<int, string>();

                _typingUsers[groupName][userId] = username;

                // Notificar a otros usuarios (excepto al remitente)
                await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserStartedTyping", new
                {
                    UserId = userId,
                    Username = username
                });

                // Actualizar actividad
                await _chatService.UpdateLastActivityAsync(Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en StartTyping para evento {eventId}");
            }
        }

        public async Task StopTyping(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();
                var groupName = GetEventGroupName(eventId);

                // Remover de la lista de usuarios escribiendo
                if (_typingUsers.ContainsKey(groupName))
                {
                    _typingUsers[groupName].Remove(userId);

                    if (_typingUsers[groupName].Count == 0)
                        _typingUsers.Remove(groupName);
                }

                // Notificar a otros usuarios (excepto al remitente)
                await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserStoppedTyping", new
                {
                    UserId = userId,
                    Username = username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en StopTyping para evento {eventId}");
            }
        }

        public async Task GetConnectedUsers(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (!await _chatService.CanUserJoinChatAsync(eventId, userId))
                {
                    await Clients.Caller.SendAsync("Error", "No tienes acceso a esta información");
                    return;
                }

                var connectedUsers = await _chatService.GetConnectedUsersAsync(eventId);
                await Clients.Caller.SendAsync("ConnectedUsersUpdated", connectedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener usuarios conectados del evento {eventId}");
                await Clients.Caller.SendAsync("Error", "Error al obtener usuarios conectados");
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();

            _logger.LogInformation($"Usuario {username} ({userId}) conectado via SignalR. ConnectionId: {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                // Remover conexión
                await _chatService.RemoveConnectionAsync(Context.ConnectionId);

                // Remover de todas las listas de usuarios escribiendo
                foreach (var group in _typingUsers.ToList())
                {
                    if (group.Value.ContainsKey(userId))
                    {
                        group.Value.Remove(userId);

                        // Notificar que dejó de escribir
                        await Clients.Group(group.Key).SendAsync("UserStoppedTyping", new
                        {
                            UserId = userId,
                            Username = username
                        });

                        if (group.Value.Count == 0)
                            _typingUsers.Remove(group.Key);
                    }
                }

                _logger.LogInformation($"Usuario {username} ({userId}) desconectado. Reason: {exception?.Message ?? "Normal disconnect"}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante desconexión");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Métodos auxiliares
        private int GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        private string GetCurrentUsername()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario Desconocido";
        }

        private string GetCurrentUserRole()
        {
            return Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        private static string GetEventGroupName(int eventId)
        {
            return $"evento-{eventId}";
        }
    }
}