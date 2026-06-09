namespace PCS_API.Models;

public class RoleModel
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}