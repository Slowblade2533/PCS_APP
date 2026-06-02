using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IProductRepository
{
    Task<int> CreateProductWithVariantsAsync(ProductCreateDto dto);
    Task<PagedResult<ProductListDto>> GetPagedProductsAsync(ProductSearchParams search);
}
