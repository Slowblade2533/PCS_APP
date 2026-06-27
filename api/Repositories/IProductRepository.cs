using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface IProductRepository
{
    Task<PagedResultDto<ProductListDto>> GetPagedProductsAsync(ProductSearchParamsDto search, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default);
    
    Task<int> InsertProductAsync(ProductCreateDto dto, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> UpdateProductAsync(int productId, ProductCreateDto dto, IDbTransaction transaction, CancellationToken cancellationToken = default);
    
    Task<int> InsertProductVariantAsync(int productId, VariantCreateDto v, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateProductVariantsAndPricesAsync(IEnumerable<object> updateDataList, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task DeleteProductVariantsAsync(IEnumerable<int> variantIds, IDbTransaction transaction, CancellationToken cancellationToken = default);

    Task InsertProductPriceAsync(int variantId, decimal basePrice, decimal? discountPrice, IDbTransaction transaction, CancellationToken cancellationToken = default);

    Task<List<string>> CheckBarcodesExistAsync(IEnumerable<string> barcodes, IEnumerable<int>? ignoreVariantIds = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<List<string>> CheckSkusExistAsync(IEnumerable<string> skus, IEnumerable<int>? ignoreVariantIds = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<List<int>> GetExistingVariantIdsAsync(int productId, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<(int VariantId, string? ImageUrl)>> GetVariantImagesAsync(IEnumerable<int> variantIds, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<(int VariantId, string Condition, byte[] RowVersion)>> GetStockRowVersionsAsync(IEnumerable<int> variantIds, IDbTransaction transaction, CancellationToken cancellationToken = default);
}

