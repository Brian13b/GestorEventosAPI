using EventManagementAPI.DTOs;
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
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        [HttpGet("events/{eventId}/messages")]
        public async Task<IActionResult> GetMessageHistory(int eventId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Límite máximo
                if (page < 1) page = 1;

                var userId = GetCurrentUserId();
                var messages = await _chatService.GetMessageHistoryAsync(eventId, userId, page, pageSize);

                return Ok(new
                {
                    message = "Historial obtenido exitosamente",
                    data = messages,
                    pagination = new
                    {
                        page,
                        pageSize,
                        hasMore = messages.Count == pageSize
                    }
                });
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

        [HttpPost("events/{eventId}/messages")]
        public async Task<IActionResult> SendMessage(int eventId, [FromBody] SendMessageDtoEnhanced messageDto)
        {
            try
            {
                if (messageDto.EventId != eventId)
                {
                    return BadRequest(new { message = "ID de evento no coincide" });
                }

                var userId = GetCurrentUserId();

                var basicDto = new DTOs.SendMessageDto
                {
                    EventId = messageDto.EventId,
                    Content = messageDto.Content
                };

                var message = await _chatService.SendMessageAsync(basicDto, userId);

                return Ok(new { message = "Mensaje enviado exitosamente", data = message });
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

        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _chatService.DeleteMessageAsync(messageId, userId);

                if (!success)
                    return NotFound(new { message = "Mensaje no encontrado" });

                return Ok(new { message = "Mensaje eliminado exitosamente" });
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

        [HttpGet("events/{eventId}/room-info")]
        public async Task<IActionResult> GetChatRoomInfo(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (!await _chatService.CanUserJoinChatAsync(eventId, userId))
                {
                    return Forbid("No tienes acceso a este chat. Debes estar registrado al evento.");
                }

                var chatRoom = await _chatService.JoinChatRoomAsync(eventId, userId);
                return Ok(new { message = "Información de la sala obtenida exitosamente", data = chatRoom });
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

        [HttpGet("events/{eventId}/connected-users")]
        public async Task<IActionResult> GetConnectedUsers(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (!await _chatService.CanUserJoinChatAsync(eventId, userId))
                {
                    return Forbid("No tienes acceso a esta información. Debes estar registrado al evento.");
                }

                var connectedUsers = await _chatService.GetConnectedUsersAsync(eventId);
                return Ok(new { message = "Usuarios conectados obtenidos exitosamente", data = connectedUsers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        [HttpGet("events/{eventId}/can-join")]
        public async Task<IActionResult> CanJoinChat(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var canJoin = await _chatService.CanUserJoinChatAsync(eventId, userId);

                return Ok(new
                {
                    message = "Verificación completada",
                    data = new { canJoin, eventId, userId }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        [HttpPost("events/{eventId}/rate-limit-check")]
        public async Task<IActionResult> CheckRateLimit(int eventId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var canSend = await _chatService.CheckRateLimitAsync(userId, eventId);

                return Ok(new
                {
                    message = "Verificación de límite completada",
                    data = new { canSendMessage = canSend, rateLimitInfo = "Máximo 10 mensajes por minuto" }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }
    }
}