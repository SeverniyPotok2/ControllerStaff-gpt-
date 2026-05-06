using ControllerStaff.Core.Abstraction;
using ControllerStaff.Core.DTOs;
using ControllerStaff.Core.Models;
using ControllerStaff.DataAccess.PassHash;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ControllerStaff.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _userRepository.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<User>> GetByEmail(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("department/{departmentId}/active")]
        public async Task<ActionResult<List<User>>> GetActiveByDepartment(int departmentId)
        {
            var users = await _userRepository.GetActiveByDepartmentAsync(departmentId);
            return Ok(users);
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<User>>> GetActiveUsers()
        {
            var allUsers = await _userRepository.GetAllAsync();
            var activeUsers = allUsers.Where(u => u.IsActive).ToList();
            return Ok(activeUsers);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateUser([FromBody] CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Email и пароль обязательны.");

            if (await _userRepository.UserExistsByEmailAsync(dto.Email))
                return BadRequest("Пользователь с таким email уже существует.");

            var user = await CreateUserFromDto(dto);
            if (user == null) return BadRequest("Не удалось создать пользователя.");

            var created = await _userRepository.CreateAsync(user);
            if (!created) return StatusCode(500, "Ошибка создания пользователя.");

            _logger.LogInformation("Пользователь создан: {Email}", dto.Email);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user.Id);
        }

        private async Task<User?> CreateUserFromDto(CreateUserDto dto)
        {
            var user = new User(
                0,
                dto.Email,
                dto.Password,
                dto.FullName,
                dto.DepartmentId,
                dto.Role,
                true,
                DateTime.UtcNow);
            return user;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            if (id != dto.Id) return BadRequest("ID в пути и в теле не совпадают.");

            var existing = await _userRepository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var updatedUser = new User(
                dto.Id,
                dto.Email,
                existing.PasswordHash,
                dto.FullName,
                dto.DepartmentId,
                dto.Role,
                dto.IsActive,
        existing.CreatedAt);

            var result = await _userRepository.UpdateAsync(updatedUser);
            if (!result) return BadRequest("Не удалось обновить пользователя.");

            _logger.LogInformation("Пользователь обновлён: {Id}", id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userRepository.DeleteAsync(id);
            if (!result) return NotFound();

            _logger.LogInformation("Пользователь удалён: {Id}", id);
            return NoContent();
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var result = await _userRepository.DeactivateAsync(id);
            if (!result) return NotFound();

            _logger.LogInformation("Пользователь деактивирован: {Id}", id);
            return NoContent();
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest("Пароль не может быть пустым.");

            var existing =
                await _userRepository.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            var passwordHash =
                PasswordHasher.HashPassword(newPassword);

            var result =
                await _userRepository.UpdatePasswordHashAsync(
                    id,
                    passwordHash
                );

            if (!result)
                return BadRequest("Не удалось обновить пароль.");

            return NoContent();
        }
    }
}