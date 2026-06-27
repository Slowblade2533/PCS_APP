using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IGoodsReceiptService
{
    Task<PagedResultDto<GoodsReceiptListDto>> GetPagedAsync(GoodsReceiptSearchDto search, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDetailDto?> GetByIdAsync(int receiptId, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateAsync(GoodsReceiptCreateDto dto, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> CompleteReceiptAsync(int receiptId, int? updatedBy, CancellationToken cancellationToken = default);
}
