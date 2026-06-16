using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IStockService
{
    Task<PagedResultDto<StockDto>> GetStockStatusAsync(StockSearchDto search, CancellationToken cancellationToken = default);
    Task<bool> ProcessStockTransactionAsync(CreateStockTransactionDto dto, int userId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<StockTransactionHistoryDto>> GetTransactionsAsync(string? transactionType, PaginationParamsDto @params, CancellationToken cancellationToken = default);
}
