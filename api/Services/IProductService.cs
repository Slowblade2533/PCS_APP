using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IProductService
{
    Task<PagedResultDto<ProductListDto>> GetPagedProductsAsync(ProductSearchParamsDto search, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateProductWithVariantsAsync(ProductCreateDto dto, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateProductWithVariantsAsync(int productId, ProductCreateDto dto, CancellationToken cancellationToken = default);
}
