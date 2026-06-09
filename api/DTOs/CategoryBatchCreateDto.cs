namespace PCS_API.DTOs;

public class CategoryBatchCreateDto
{
    public string Level1Name { get; set; } = string.Empty;
    public string? Level2Name { get; set; }
    public string? Level3Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
