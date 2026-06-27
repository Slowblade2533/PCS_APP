using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface ITaxInvoiceRepository
{
    Task<PagedResultDto<TaxInvoiceDto>> GetTaxInvoicesPagedAsync(TaxInvoiceSearchDto search, CancellationToken cancellationToken = default);
    Task<TaxInvoiceDto?> GetTaxInvoiceByIdAsync(int taxInvoiceId, CancellationToken cancellationToken = default);
    Task<int> InsertTaxInvoiceAsync(TaxInvoiceCreateDto dto, CancellationToken cancellationToken = default);
}
