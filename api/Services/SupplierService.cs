using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<ResultDto<PagedResultDto<SupplierDto>>> GetPagedAsync(SupplierSearchDto search, CancellationToken cancellationToken = default)
    {
        var result = await _supplierRepository.GetPagedAsync(search, cancellationToken);
        return ResultDto<PagedResultDto<SupplierDto>>.Success(result);
    }

    public async Task<ResultDto<SupplierDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
            return ResultDto<SupplierDto>.Failure("ไม่พบข้อมูลผู้จำหน่าย");

        var dto = new SupplierDto
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxId = supplier.TaxId,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };

        return ResultDto<SupplierDto>.Success(dto);
    }

    public async Task<ResultDto<int>> CreateAsync(SupplierCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (await _supplierRepository.ExistsByCodeAsync(dto.SupplierCode, cancellationToken: cancellationToken))
            return ResultDto<int>.Failure("รหัสผู้จำหน่ายซ้ำในระบบ");

        var model = new SupplierModel
        {
            SupplierCode = dto.SupplierCode,
            SupplierName = dto.SupplierName,
            ContactName = dto.ContactName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            TaxId = dto.TaxId,
            IsActive = dto.IsActive
        };

        int newId = await _supplierRepository.CreateAsync(model, cancellationToken);
        return ResultDto<int>.Success(newId);
    }

    public async Task<ResultDto<bool>> UpdateAsync(int id, SupplierCreateDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return ResultDto<bool>.Failure("ไม่พบข้อมูลผู้จำหน่าย");

        if (await _supplierRepository.ExistsByCodeAsync(dto.SupplierCode, excludeId: id, cancellationToken: cancellationToken))
            return ResultDto<bool>.Failure("รหัสผู้จำหน่ายซ้ำในระบบ");

        existing.SupplierCode = dto.SupplierCode;
        existing.SupplierName = dto.SupplierName;
        existing.ContactName = dto.ContactName;
        existing.Phone = dto.Phone;
        existing.Email = dto.Email;
        existing.Address = dto.Address;
        existing.TaxId = dto.TaxId;
        existing.IsActive = dto.IsActive;

        await _supplierRepository.UpdateAsync(existing, cancellationToken);
        return ResultDto<bool>.Success(true);
    }
}
