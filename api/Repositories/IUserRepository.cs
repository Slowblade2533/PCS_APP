using PCS_API.DTOs;
using PCS_API.Models;

namespace PCS_API.Repositories;

public interface IUserRepository
{
    Task<UserTableModel?> GetActiveUserByEmailAsync(string email);
    Task<IEnumerable<UserPermissionInfoModel>> GetUserPermissionsAsync(int userId);
    
    Task<(IEnumerable<UserListItemDto> Items, int TotalCount)> GetUsersAsync(UserSearchDto search, CancellationToken cancellationToken = default);
    Task<UserDetailDto?> GetUserByIdAsync(int id);
    Task<int> CreateUserAsync(UserTableModel user, IEnumerable<UserPermissionAssignmentRequestDto> permissions);
    Task UpdateUserAsync(int id, UserTableModel user, IEnumerable<UserPermissionAssignmentRequestDto> permissions);
    Task ToggleActiveAsync(int id, bool isActive);
    Task<IEnumerable<RoleDto>> GetRolesAsync();
    Task<IEnumerable<PermissionDto>> GetPermissionsAsync();
}