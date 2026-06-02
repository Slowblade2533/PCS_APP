using PCS_API.Models;

namespace PCS_API.Repositories;

public interface IUserRepository
{
    Task<UserTable?> GetActiveUserByEmailAsync(string email);
}
