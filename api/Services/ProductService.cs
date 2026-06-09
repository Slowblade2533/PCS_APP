using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<ResultDto<int>> CreateProductAsync(ProductCreateDto dto)
    {
        return await productRepository.CreateProductWithVariantsAsync(dto);
    }

    public async Task<PagedResultDto<ProductListDto>> GetProductsAsync(ProductSearchParamsDto search)
    {
        return await productRepository.GetPagedProductsAsync(search);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
    {
        return await productRepository.GetProductDetailAsync(id);
    }

    public async Task<ResultDto<bool>> UpdateProductAsync(int id, ProductCreateDto dto)
    {
        return await productRepository.UpdateProductWithVariantsAsync(id, dto);
    }
}
