using Dapper;
using PCS_API.DTOs;
using PCS_API.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCS_API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    public UserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserTableModel?> GetActiveUserByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Email, Username, PasswordHash, FullName FROM Users WHERE Email = @Email AND IsActive = 1";

        return await connection.QuerySingleOrDefaultAsync<UserTableModel>(sql, new { Email = email });
    }

    public async Task<UserTableModel?> GetUserByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Email, Username, PasswordHash, FullName FROM Users WHERE Email = @Email";

        return await connection.QuerySingleOrDefaultAsync<UserTableModel>(sql, new { Email = email });
    }

    public async Task<IEnumerable<UserPermissionInfoModel>> GetUserPermissionsAsync(int userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT r.RoleName, p.PermissionCode, 'Global' as ScopeType, NULL as ScopeId
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON u.RoleId = r.Id
            INNER JOIN dbo.RolePermissions rp ON r.Id = rp.RoleId
            INNER JOIN dbo.Permissions p ON rp.PermissionId = p.Id
            WHERE u.Id = @UserId
            UNION
            SELECT 'Custom' as RoleName, p.PermissionCode, 'Global' as ScopeType, NULL as ScopeId
            FROM dbo.UserPermissions up
            INNER JOIN dbo.Permissions p ON up.PermissionId = p.Id
            WHERE up.UserId = @UserId";

        return await conn.QueryAsync<UserPermissionInfoModel>(sql, new { UserId = userId });
    }

    public async Task<(IEnumerable<UserListItemDto> Items, int TotalCount)> GetUsersAsync(UserSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();

        var whereClause = "WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            whereClause += " AND (u.FullName LIKE '%' + @Search + '%' OR u.Email LIKE '%' + @Search + '%' OR u.Username LIKE '%' + @Search + '%')";
        }
        if (search.IsActive.HasValue)
        {
            whereClause += " AND u.IsActive = @IsActive";
        }

        var countSql = $"SELECT COUNT(1) FROM dbo.Users u {whereClause}";
        var countCommand = new CommandDefinition(countSql, search, cancellationToken: cancellationToken);
        var totalCount = await conn.ExecuteScalarAsync<int>(countCommand);

        var dataSql = $@"
            SELECT u.Id, u.Username, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.RoleId, r.RoleName
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON u.RoleId = r.Id
            {whereClause}
            ORDER BY u.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        ";

        var p = new DynamicParameters(search);
        p.Add("@Offset", search.GetSafeOffset());
        
        var dataCommand = new CommandDefinition(dataSql, p, cancellationToken: cancellationToken);
        var users = await conn.QueryAsync<dynamic>(dataCommand);
        
        if (!users.Any())
        {
            return (new List<UserListItemDto>(), totalCount);
        }

        var userIds = users.Select(u => (int)u.Id).ToList();

        var permsSql = @"
            SELECT up.UserId, up.PermissionId, p.PermissionCode
            FROM dbo.UserPermissions up
            INNER JOIN dbo.Permissions p ON up.PermissionId = p.Id
            WHERE up.UserId IN @UserIds";
            
        var permsCommand = new CommandDefinition(permsSql, new { UserIds = userIds }, cancellationToken: cancellationToken);
        var userPerms = await conn.QueryAsync<dynamic>(permsCommand);

        var items = users.Select(u => 
        {
            var fullName = (string)u.FullName ?? "";
            var nameParts = fullName.Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var dto = new UserListItemDto
            {
                Id = (int)u.Id,
                Username = (string)u.Username,
                Email = (string)u.Email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = (bool)u.IsActive,
                CreatedAt = u.CreatedAt,
                RoleId = (int)u.RoleId,
                RoleName = (string)u.RoleName,
                Permissions = userPerms.Where(up => (int)up.UserId == (int)u.Id)
                                 .Select(up => new UserPermissionSummaryDto
                                 {
                                     PermissionId = (int)up.PermissionId,
                                     PermissionCode = (string)up.PermissionCode
                                 }).ToList()
            };
            return dto;
        }).ToList();

        return (items, totalCount);
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();

        var dataSql = @"
            SELECT u.Id, u.Username, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.RoleId, r.RoleName 
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON u.RoleId = r.Id 
            WHERE u.Id = @Id";
        var user = await conn.QuerySingleOrDefaultAsync<dynamic>(dataSql, new { Id = id });

        if (user == null) return null;

        var permsSql = @"
            SELECT up.PermissionId, p.PermissionCode
            FROM dbo.UserPermissions up
            INNER JOIN dbo.Permissions p ON up.PermissionId = p.Id
            WHERE up.UserId = @Id";

        var perms = await conn.QueryAsync<UserPermissionSummaryDto>(permsSql, new { Id = id });

        var fullName = (string)user.FullName ?? "";
        var nameParts = fullName.Split(' ', 2);
        
        return new UserDetailDto
        {
            Id = (int)user.Id,
            Username = (string)user.Username,
            Email = (string)user.Email,
            FirstName = nameParts.Length > 0 ? nameParts[0] : "",
            LastName = nameParts.Length > 1 ? nameParts[1] : "",
            IsActive = (bool)user.IsActive,
            CreatedAt = user.CreatedAt,
            RoleId = (int)user.RoleId,
            RoleName = (string)user.RoleName,
            Permissions = perms.ToList()
        };
    }

    public async Task<int> CreateUserAsync(UserTableModel user, IEnumerable<UserPermissionAssignmentRequestDto> permissions)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var userSql = @"
                INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, IsActive, CreatedAt, RoleId)
                OUTPUT INSERTED.Id
                VALUES (@Username, @Email, @PasswordHash, @FullName, @IsActive, GETDATE(), @RoleId)";
                
            var userId = await conn.ExecuteScalarAsync<int>(userSql, user, tx);

            if (permissions.Any())
            {
                var permSql = @"
                    INSERT INTO dbo.UserPermissions (UserId, PermissionId, CreatedAt)
                    VALUES (@UserId, @PermissionId, GETDATE())";

                var permParams = permissions.Select(p => new {
                    UserId = userId,
                    PermissionId = p.PermissionId
                });

                await conn.ExecuteAsync(permSql, permParams, tx);
            }

            tx.Commit();
            return userId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateUserAsync(int id, UserTableModel user, IEnumerable<UserPermissionAssignmentRequestDto> permissions)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var userSql = @"
                UPDATE dbo.Users 
                SET Email = @Email, FullName = @FullName, IsActive = @IsActive, RoleId = @RoleId
                WHERE Id = @Id";
                
            await conn.ExecuteAsync(userSql, new { user.Email, user.FullName, user.IsActive, user.RoleId, Id = id }, tx);

            await conn.ExecuteAsync("DELETE FROM dbo.UserPermissions WHERE UserId = @Id", new { Id = id }, tx);

            if (permissions.Any())
            {
                var permSql = @"
                    INSERT INTO dbo.UserPermissions (UserId, PermissionId, CreatedAt)
                    VALUES (@UserId, @PermissionId, GETDATE())";

                var permParams = permissions.Select(p => new {
                    UserId = id,
                    PermissionId = p.PermissionId
                });

                await conn.ExecuteAsync(permSql, permParams, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task ToggleActiveAsync(int id, bool isActive)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE dbo.Users SET IsActive = @IsActive WHERE Id = @Id", new { Id = id, IsActive = isActive });
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<RoleDto>("SELECT Id, RoleName, Description FROM dbo.Roles ORDER BY RoleName");
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<PermissionDto>("SELECT Id, PermissionCode, Description FROM dbo.Permissions ORDER BY PermissionCode");
    }
}