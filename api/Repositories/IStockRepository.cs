using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface IStockRepository
{
    Task<PagedResultDto<StockTransactionHistoryDto>> GetTransactionsAsync(string? transactionType, PaginationParamsDto @params);
    Task<PagedResultDto<StockDto>> GetStocksPagedAsync(StockSearchDto search);
    Task<(int Before, int After)> UpdateStockQuantityAsync(int variantId, string transactionType, int qtyChange, IDbTransaction transaction);
    Task<bool> VariantExistsAsync(int variantId, IDbTransaction transaction);
    Task<bool> TransactionExistsByRequestIdAsync(Guid requestId, IDbTransaction transaction);
    Task<int> CreateTransactionAsync(PCS_API.Models.StockTransactionModel tx, IDbTransaction transaction);
}