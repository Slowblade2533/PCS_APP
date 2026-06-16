using PCS_API.Models;

namespace PCS_API.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync(string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<CategoryModel?> GetCategoryByIdAsync(int id);
    Task<int> CreateCategoryAsync(CategoryModel category);
    Task<bool> UpdateCategoryAsync(CategoryModel category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> HasChildrenAsync(int id);
}
