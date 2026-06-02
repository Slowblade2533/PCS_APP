using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;
    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException(nameof(configuration));
    }
    public async Task<UserTable?> GetActiveUserByEmailAsync(string email)
    {
        using var connection = new SqlConnection(_connectionString);
        string sql = "SELECT Id, Email, PasswordHash, FullName FROM Users WHERE Email = @Email AND IsActive = 1";

        return await connection.QuerySingleOrDefaultAsync<UserTable>(sql, new { Email = email });
    }
}
