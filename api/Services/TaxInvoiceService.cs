using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class TaxInvoiceService(ITaxInvoiceRepository taxInvoiceRepo) : ITaxInvoiceService
{
    public async Task<PagedResultDto<TaxInvoiceDto>> GetTaxInvoicesPagedAsync(TaxInvoiceSearchDto search, CancellationToken cancellationToken = default)
    {
        return await taxInvoiceRepo.GetTaxInvoicesPagedAsync(search, cancellationToken);
    }

    public async Task<TaxInvoiceDto?> GetTaxInvoiceByIdAsync(int taxInvoiceId, CancellationToken cancellationToken = default)
    {
        return await taxInvoiceRepo.GetTaxInvoiceByIdAsync(taxInvoiceId, cancellationToken);
    }

    public async Task<ResultDto<int>> CreateTaxInvoiceAsync(TaxInvoiceCreateDto dto, CancellationToken cancellationToken = default)
    {
        // Add business validation
        if (dto.BaseAmount < 0 || dto.VatAmount < 0 || dto.TotalAmount < 0)
        {
            return ResultDto<int>.Failure("จำนวนเงินไม่สามารถติดลบได้");
        }

        // Verify calculation (allowing a tiny float rounding margin)
        decimal expectedVat = Math.Round(dto.BaseAmount * dto.VatRate / 100m, 2);
        decimal expectedTotal = dto.BaseAmount + dto.VatAmount;
        
        if (Math.Abs(dto.TotalAmount - expectedTotal) > 0.05m)
        {
            return ResultDto<int>.Failure("ยอดรวม (TotalAmount) ไม่ตรงกับ BaseAmount + VatAmount");
        }

        int newId = await taxInvoiceRepo.InsertTaxInvoiceAsync(dto, cancellationToken);
        return ResultDto<int>.Success(newId);
    }
}
