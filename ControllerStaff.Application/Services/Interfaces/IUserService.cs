using ControllerStaff.Core.DTO;
using ControllerStaff.Core.DTOs;

namespace ControllerStaff.Application.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync();

    Task<UserResponseDto?> GetByIdAsync(int id);

    Task<UserResponseDto?> CreateAsync(CreateUserDto dto);

    Task<bool> UpdateAsync(int id, UpdateUserDto dto);

    Task<bool> DeleteAsync(int id);

    Task<bool> UpdatePasswordAsync(
        int id,
        string newPassword
    );
}