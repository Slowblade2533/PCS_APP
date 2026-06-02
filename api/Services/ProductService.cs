using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    public async Task<int> CreateProductAsync(ProductCreateDto dto)
    {
        return await _productRepository.CreateProductWithVariantsAsync(dto);
    }

    public async Task<PagedResult<ProductListDto>> GetProductsAsync(ProductSearchParams search)
    {
        return await _productRepository.GetPagedProductsAsync(search);
    }
}
