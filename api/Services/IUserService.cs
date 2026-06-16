using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IUserService
{
    Task<PagedResultDto<UserListItemDto>> GetUsersAsync(UserSearchDto search, CancellationToken cancellationToken = default);
    Task<UserDetailDto?> GetUserByIdAsync(int id);
    Task<UserDetailDto> CreateUserAsync(UserCreateRequestDto request);
    Task<UserDetailDto> UpdateUserAsync(int id, UserUpdateRequestDto request);
    Task ToggleActiveAsync(int id, bool isActive);
    Task<IEnumerable<RoleDto>> GetRolesAsync();
    Task<IEnumerable<PermissionDto>> GetPermissionsAsync();
}
