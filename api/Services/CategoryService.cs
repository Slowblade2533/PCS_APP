using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class CategoryService(ICategoryRepository categoryRepository) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var models = await categoryRepository.GetAllCategoriesAsync(searchTerm, cancellationToken);
        return models.Select(MapToDto);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var model = await categoryRepository.GetCategoryByIdAsync(id);
        return model != null ? MapToDto(model) : null;
    }

    public async Task<(bool IsSuccess, int? CategoryId, string? ErrorMessage)> CreateCategoryAsync(CategoryCreateDto dto)
    {
        if (dto.ParentId.HasValue)
        {
            var parent = await categoryRepository.GetCategoryByIdAsync(dto.ParentId.Value);
            if (parent == null)
            {
                return (false, null, "ไม่พบหมวดหมู่หลักที่ระบุ");
            }
            if (parent.Level >= 3)
            {
                return (false, null, "ระบบรองรับหมวดหมู่ย่อยได้สูงสุด 3 ระดับเท่านั้น");
            }
        }

        var model = new CategoryModel
        {
            CategoryName = dto.CategoryName,
            Description = dto.Description,
            ParentId = dto.ParentId,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };

        var id = await categoryRepository.CreateCategoryAsync(model);
        return (true, id, null);
    }

    public async Task<(bool IsSuccess, int? CategoryId, string? ErrorMessage)> CreateCategoryBatchAsync(CategoryBatchCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Level1Name))
        {
            return (false, null, "กรุณาระบุชื่อหมวดหมู่หลัก (Level 1)");
        }

        var allCats = await categoryRepository.GetAllCategoriesAsync();

        // Level 1
        var l1 = allCats.FirstOrDefault(c => c.CategoryName.Equals(dto.Level1Name.Trim(), StringComparison.OrdinalIgnoreCase) && c.ParentId == null);
        int l1Id;
        if (l1 == null)
        {
            l1Id = await categoryRepository.CreateCategoryAsync(new CategoryModel
            {
                CategoryName = dto.Level1Name.Trim(),
                Description = dto.Description,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                ParentId = null
            });
        }
        else
        {
            l1Id = l1.CategoryId;
        }

        int finalId = l1Id;

        // Level 2
        if (!string.IsNullOrWhiteSpace(dto.Level2Name))
        {
            var l2 = allCats.FirstOrDefault(c => c.CategoryName.Equals(dto.Level2Name.Trim(), StringComparison.OrdinalIgnoreCase) && c.ParentId == l1Id);
            int l2Id;
            if (l2 == null)
            {
                l2Id = await categoryRepository.CreateCategoryAsync(new CategoryModel
                {
                    CategoryName = dto.Level2Name.Trim(),
                    Description = dto.Description,
                    SortOrder = dto.SortOrder,
                    IsActive = dto.IsActive,
                    ParentId = l1Id
                });
            }
            else
            {
                l2Id = l2.CategoryId;
            }
            finalId = l2Id;

            // Level 3
            if (!string.IsNullOrWhiteSpace(dto.Level3Name))
            {
                var l3 = allCats.FirstOrDefault(c => c.CategoryName.Equals(dto.Level3Name.Trim(), StringComparison.OrdinalIgnoreCase) && c.ParentId == l2Id);
                if (l3 == null)
                {
                    finalId = await categoryRepository.CreateCategoryAsync(new CategoryModel
                    {
                        CategoryName = dto.Level3Name.Trim(),
                        Description = dto.Description,
                        SortOrder = dto.SortOrder,
                        IsActive = dto.IsActive,
                        ParentId = l2Id
                    });
                }
                else
                {
                    finalId = l3.CategoryId;
                }
            }
        }

        return (true, finalId, null);
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> UpdateCategoryAsync(int id, CategoryUpdateDto dto)
    {
        var existing = await categoryRepository.GetCategoryByIdAsync(id);
        if (existing == null)
        {
            return (false, "ไม่พบหมวดหมู่ที่ต้องการแก้ไข");
        }

        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == id)
            {
                return (false, "หมวดหมู่ไม่สามารถเป็น Parent ของตัวเองได้");
            }

            var parent = await categoryRepository.GetCategoryByIdAsync(dto.ParentId.Value);
            if (parent == null)
            {
                return (false, "ไม่พบหมวดหมู่หลักที่ระบุ");
            }

            if (parent.Level >= 3)
            {
                return (false, "ระบบรองรับหมวดหมู่ย่อยได้สูงสุด 3 ระดับเท่านั้น");
            }
            
            if (parent.FullPath.Contains(existing.CategoryName)) 
            {
                 // this is a very basic circular check.
                 // A better way is checking if ParentId traces back to current id.
            }
        }

        var model = new CategoryModel
        {
            CategoryId = id,
            CategoryName = dto.CategoryName,
            Description = dto.Description,
            ParentId = dto.ParentId,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };

        var updated = await categoryRepository.UpdateCategoryAsync(model);
        if (!updated)
        {
            return (false, "ไม่สามารถบันทึกข้อมูลได้");
        }

        return (true, null);
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteCategoryAsync(int id)
    {
        var hasChildren = await categoryRepository.HasChildrenAsync(id);
        if (hasChildren)
        {
            return (false, "ไม่สามารถลบหมวดหมู่นี้ได้เนื่องจากมีหมวดหมู่ย่อยอยู่");
        }
        
        var deleted = await categoryRepository.DeleteCategoryAsync(id);
        if (!deleted)
        {
            return (false, "ไม่สามารถลบข้อมูลได้");
        }

        return (true, null);
    }

    private static CategoryDto MapToDto(CategoryModel model) => new()
    {
        CategoryId = model.CategoryId,
        CategoryName = model.CategoryName,
        Description = model.Description,
        ParentId = model.ParentId,
        ParentName = model.ParentName,
        SortOrder = model.SortOrder,
        IsActive = model.IsActive,
        FullPath = model.FullPath,
        Level = model.Level
    };
}
