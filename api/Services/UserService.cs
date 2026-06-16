using Microsoft.AspNetCore.Identity;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<UserTableModel> _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher<UserTableModel> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResultDto<UserListItemDto>> GetUsersAsync(UserSearchDto search, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _userRepository.GetUsersAsync(search, cancellationToken);
        return new PagedResultDto<UserListItemDto> 
        { 
            Items = items, 
            TotalCount = totalCount, 
            PageNumber = search.PageNumber, 
            PageSize = search.PageSize 
        };
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int id)
    {
        return await _userRepository.GetUserByIdAsync(id);
    }

    public async Task<UserDetailDto> CreateUserAsync(UserCreateRequestDto request)
    {
        var existingUser = await _userRepository.GetActiveUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new Exception("Email is already in use.");
        }

        var fullName = $"{request.FirstName} {request.LastName}".Trim();

        var newUser = new UserTableModel
        {
            Username = request.Username,
            Email = request.Email,
            FullName = fullName,
            IsActive = request.IsActive,
            RoleId = request.RoleId
        };

        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);

        var userId = await _userRepository.CreateUserAsync(newUser, request.PermissionAssignments);

        return await _userRepository.GetUserByIdAsync(userId) ?? throw new Exception("Failed to retrieve created user.");
    }

    public async Task<UserDetailDto> UpdateUserAsync(int id, UserUpdateRequestDto request)
    {
        var user = await _userRepository.GetUserByIdAsync(id) ?? throw new Exception("User not found.");

        var fullName = $"{request.FirstName} {request.LastName}".Trim();

        var updatedUser = new UserTableModel
        {
            Email = request.Email,
            FullName = fullName,
            IsActive = request.IsActive,
            RoleId = request.RoleId
        };

        await _userRepository.UpdateUserAsync(id, updatedUser, request.PermissionAssignments);

        return await _userRepository.GetUserByIdAsync(id) ?? throw new Exception("Failed to retrieve updated user.");
    }

    public async Task ToggleActiveAsync(int id, bool isActive)
    {
        await _userRepository.ToggleActiveAsync(id, isActive);
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        return await _userRepository.GetRolesAsync();
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsAsync()
    {
        return await _userRepository.GetPermissionsAsync();
    }
}
