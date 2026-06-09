using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IProductService
{
    Task<ResultDto<int>> CreateProductAsync(ProductCreateDto dto);
    Task<PagedResultDto<ProductListDto>> GetProductsAsync(ProductSearchParamsDto search);
    Task<ProductDetailDto?> GetProductByIdAsync(int id);
    Task<ResultDto<bool>> UpdateProductAsync(int id, ProductCreateDto dto);
}
