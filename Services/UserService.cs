using EventManagementAPI.Data;
using EventManagementAPI.DTOs;
using EventManagementAPI.Models;
using EventManagementAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagementAPI.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // Obtener todos los usuarios
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role.Name
            });
        }

        // Obtener usuario por ID
        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("Usuario no encontrado");
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.Name
            };
        }

        // Crear nuevo usuario
        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == createUserDto.Role);
            if (role == null) throw new KeyNotFoundException("Rol no encontrado");
            var user = new User
            {
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                RoleId = role.Id
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = role.Name
            };
        }

        // Actualizar usuario
        public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("Usuario no encontrado");
            if (!string.IsNullOrEmpty(updateUserDto.Username))
                user.Username = updateUserDto.Username;
            if (!string.IsNullOrEmpty(updateUserDto.Email))
                user.Email = updateUserDto.Email;
            if (!string.IsNullOrEmpty(updateUserDto.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);
            await _context.SaveChangesAsync();
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.Name
            };
        }

        // Eliminar usuario
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("Usuario no encontrado");
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
