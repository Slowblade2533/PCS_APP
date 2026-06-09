using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IStockService
{
    Task<PagedResultDto<StockDto>> GetStockStatusAsync(StockSearchDto search);
    Task<bool> ProcessStockTransactionAsync(CreateStockTransactionDto dto, int userId);
    Task<PagedResultDto<StockTransactionHistoryDto>> GetTransactionsAsync(string? transactionType, PaginationParamsDto @params);
}
