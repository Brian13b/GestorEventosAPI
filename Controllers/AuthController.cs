using EventManagementAPI.DTOs.Enhanced;
using EventManagementAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDtoEnhanced registerDto)
        {
            try
            {
                var basicDto = new DTOs.RegisterDto
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    Password = registerDto.Password
                };

                var response = await _authService.RegisterAsync(basicDto);
                return Ok(new { message = "Usuario registrado exitosamente", data = response });
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDtoEnhanced loginDto)
        {
            try
            {
                var basicDto = new DTOs.LoginDto
                {
                    Email = loginDto.Email,
                    Password = loginDto.Password
                };

                var response = await _authService.LoginAsync(basicDto);

                // Asumiendo que response.Token y response.User existen y tienen los datos correctos
                return Ok(new
                {
                    success = true,
                    token = response.Token,
                    user = new
                    {
                        id = response.User.Id,
                        email = response.User.Email,
                        role = response.User.Role
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error interno del servidor", details = ex.Message });
            }
        }

        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken([FromBody] string token)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(token);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }
    }
}
