using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class UserSearchDto : PaginationParamsDto
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class UserPermissionSummaryDto
{
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
}

public class UserListItemDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<UserPermissionSummaryDto> Permissions { get; set; } = new();
}

public class UserDetailDto : UserListItemDto
{
    public DateTime? UpdatedAt { get; set; }
}

public class UserPermissionAssignmentRequestDto
{
    [Required]
    public int PermissionId { get; set; }
}

public class UserCreateRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    [Required]
    public int RoleId { get; set; }
    public List<UserPermissionAssignmentRequestDto> PermissionAssignments { get; set; } = new();
}

public class UserUpdateRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    [Required]
    public int RoleId { get; set; }
    public List<UserPermissionAssignmentRequestDto> PermissionAssignments { get; set; } = new();
}

public class RoleDto
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class PermissionDto
{
    public int Id { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string? Description { get; set; }
}
