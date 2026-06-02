using PCS_API.Models;

namespace PCS_API.Services;

public interface IAuthService
{
    Task<UserTable?> AuthenticateAsync(string email, string password);
}
