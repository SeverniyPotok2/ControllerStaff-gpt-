using ControllerStaff.Application.Services.Interfaces;
using ControllerStaff.Core.Abstraction;
using ControllerStaff.Core.DTO;
using ControllerStaff.Core.DTOs;
using ControllerStaff.Core.Models;
using ControllerStaff.DataAccess.PassHash;

namespace ControllerStaff.Application.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserResponseDto>>
        GetAllAsync()
    {
        var users =
            await _userRepository.GetAllAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserResponseDto?>
        GetByIdAsync(int id)
    {
        var user =
            await _userRepository.GetByIdAsync(id);

        if (user == null)
            return null;

        return MapToDto(user);
    }

    public async Task<UserResponseDto?>
        CreateAsync(CreateUserDto dto)
    {
        var passwordHash =
            PasswordHasher.HashPassword(dto.Password);

        var user = new User(
            0,
            dto.Email,
            passwordHash,
            dto.Role,
            dto.DepartmentId,
            true,
            DateTime.UtcNow
        );

        var created =
            await _userRepository.CreateAsync(user);

        if (!created)
            return null;

        var createdUser =
            await _userRepository
                .GetByEmailAsync(dto.Email);

        if (createdUser == null)
            return null;

        return MapToDto(createdUser);
    }

    public async Task<bool>
        UpdateAsync(int id, UpdateUserDto dto)
    {
        var existing =
            await _userRepository.GetByIdAsync(id);

        if (existing == null)
            return false;

        var updatedUser = new User(
            id,
            dto.Email,
            existing.PasswordHash,
            dto.Role,
            dto.DepartmentId,
            dto.IsActive
        );

        return await _userRepository
            .UpdateAsync(updatedUser);
    }

    public async Task<bool>
        DeleteAsync(int id)
    {
        return await _userRepository
            .DeleteAsync(id);
    }

    public async Task<bool>
        UpdatePasswordAsync(
            int id,
            string newPassword)
    {
        var existing =
            await _userRepository.GetByIdAsync(id);

        if (existing == null)
            return false;

        var passwordHash =
            PasswordHasher.HashPassword(newPassword);

        return await _userRepository
            .UpdatePasswordHashAsync(
                id,
                passwordHash
            );
    }

    private static UserResponseDto
        MapToDto(User user)
    {
        return new UserResponseDto(
            user.Id,
            user.Email,
            user.Role,
            user.DepartmentId,
            user.IsActive
        );
    }
}