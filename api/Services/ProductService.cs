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

    public async Task<object?> GetProductByIdAsync(int id)
    {
        return await _productRepository.GetProductDetailAsync(id);
    }

    public async Task<bool> UpdateProductAsync(int id, ProductCreateDto dto)
    {
        return await _productRepository.UpdateProductWithVariantsAsync(id, dto);
    }
}
