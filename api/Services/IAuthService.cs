using PCS_API.Models;

namespace PCS_API.Services;

public interface IAuthService
{
    Task<UserTableModel?> AuthenticateAsync(string email, string password);
    Task<IEnumerable<UserPermissionInfoModel>> GetUserPermissionsAsync(int userId);
}
