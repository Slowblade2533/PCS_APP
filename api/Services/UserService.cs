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
        // Check all users (active AND inactive) to prevent duplicate email accounts.
        // Without this, a deactivated user's email could be silently re-used.
        var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email นี้ถูกใช้งานอยู่แล้วในระบบ");
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

        return await _userRepository.GetUserByIdAsync(userId) 
            ?? throw new InvalidOperationException("ไม่สามารถดึงข้อมูลผู้ใช้ที่สร้างใหม่ได้");
    }

    public async Task<UserDetailDto> UpdateUserAsync(int id, UserUpdateRequestDto request)
    {
        var user = await _userRepository.GetUserByIdAsync(id) 
            ?? throw new KeyNotFoundException("ไม่พบข้อมูลผู้ใช้");

        var fullName = $"{request.FirstName} {request.LastName}".Trim();

        var updatedUser = new UserTableModel
        {
            Email = request.Email,
            FullName = fullName,
            IsActive = request.IsActive,
            RoleId = request.RoleId
        };

        await _userRepository.UpdateUserAsync(id, updatedUser, request.PermissionAssignments);

        return await _userRepository.GetUserByIdAsync(id) 
            ?? throw new InvalidOperationException("ไม่สามารถดึงข้อมูลผู้ใช้หลังแก้ไขได้");
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
