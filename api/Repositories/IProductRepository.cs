using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IProductRepository
{
    Task<ResultDto<int>> CreateProductWithVariantsAsync(ProductCreateDto dto);
    Task<PagedResultDto<ProductListDto>> GetPagedProductsAsync(ProductSearchParamsDto search);
    Task<ProductDetailDto?> GetProductDetailAsync(int productId);
    Task<ResultDto<bool>> UpdateProductWithVariantsAsync(int productId, ProductCreateDto dto);
}