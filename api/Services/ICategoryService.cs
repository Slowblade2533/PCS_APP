using PCS_API.DTOs;

namespace PCS_API.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(string? searchTerm = null);
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<(bool IsSuccess, int? CategoryId, string? ErrorMessage)> CreateCategoryAsync(CategoryCreateDto dto);
    Task<(bool IsSuccess, int? CategoryId, string? ErrorMessage)> CreateCategoryBatchAsync(CategoryBatchCreateDto dto);
    Task<(bool IsSuccess, string? ErrorMessage)> UpdateCategoryAsync(int id, CategoryUpdateDto dto);
    Task<(bool IsSuccess, string? ErrorMessage)> DeleteCategoryAsync(int id);
}
