using PCS_API.DTOs;

namespace PCS_API.Services;

public interface ITaxInvoiceService
{
    Task<PagedResultDto<TaxInvoiceDto>> GetTaxInvoicesPagedAsync(TaxInvoiceSearchDto search, CancellationToken cancellationToken = default);
    Task<TaxInvoiceDto?> GetTaxInvoiceByIdAsync(int taxInvoiceId, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateTaxInvoiceAsync(TaxInvoiceCreateDto dto, CancellationToken cancellationToken = default);
}
