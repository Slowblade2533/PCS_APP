using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IProductService
{
    Task<int> CreateProductAsync(ProductCreateDto dto);
    Task<PagedResult<ProductListDto>> GetProductsAsync(ProductSearchParams search);
    Task<object?> GetProductByIdAsync(int id);
    Task<bool> UpdateProductAsync(int id, ProductCreateDto dto);
}
