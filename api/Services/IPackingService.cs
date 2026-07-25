using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IPackingService
{
    Task<PackingBatchResponseDto> CreateDraftBatchAsync(CreatePackingBatchDto dto, int? userId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<PackingBatchResponseDto>> GetBatchesPagedAsync(PaginationParamsDto @params, CancellationToken cancellationToken = default);
    Task<PackingBatchResponseDto?> GetBatchByIdAsync(int batchId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, SkuCheckResultDto>> CheckSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
    Task<bool> ConfirmShipmentAsync(ConfirmShipmentDto dto, int? userId, CancellationToken cancellationToken = default);
    Task<List<PackingBatchItemResponseDto>> GetPendingBackordersAsync(CancellationToken cancellationToken = default);
    Task<bool> ResolveBackorderAsync(ResolveBackorderDto dto, int? userId, CancellationToken cancellationToken = default);
}

public class SkuCheckResultDto
{
    public bool IsMatched { get; set; }
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
}
