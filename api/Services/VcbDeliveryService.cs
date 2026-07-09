using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class VcbDeliveryService(
        IVcbDeliveryRepository repository,
        IWebHostEnvironment env,
        IFinancialTransactionService financialTransactionService,
        IAccountTransactionRepository accountTransactionRepo,
        ISqlConnectionFactory connectionFactory,
        ILogger<VcbDeliveryService> logger) : IVcbDeliveryService
{
    public async Task<PagedResultDto<VcbDeliveryDto>> GetPagedAsync(VcbDeliverySearchDto search, CancellationToken cancellationToken = default)
    {
        return await repository.GetPagedAsync(search, cancellationToken);
    }

    public async Task<VcbDeliveryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<int> CreateAsync(VcbDeliveryCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        string? transferSlipUrl = null;

        if (slipFile != null && slipFile.Length > 0)
        {
            // Validate size (max 5MB)
            if (slipFile.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("ขนาดไฟล์ต้องไม่เกิน 5MB");
            }

            // Validate extension
            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
            {
                throw new ArgumentException("รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");
            }

            string uploadDir = Path.Combine(env.WebRootPath, "uploads", "slips", "deliveries");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            string fileName = $"slip_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await slipFile.CopyToAsync(stream, cancellationToken);
            }

            transferSlipUrl = $"/uploads/slips/deliveries/{fileName}";
        }

        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            int newId = await repository.CreateAsync(dto, transferSlipUrl, currentUserId, tx, cancellationToken);

            if (dto.TransferredAmount > 0)
            {
                // Sync Expense (AccountTransaction)
                await accountTransactionRepo.CreateAsync(new Models.AccountTransactionModel
                {
                    TransactionDate = DateTime.Now,
                    Type = "Expense",
                    Amount = dto.TransferredAmount,
                    ReferenceType = "VcbDeliveryCost",
                    ReferenceId = newId,
                    Notes = $"ค่าขนส่ง VCANBUY บิล: {dto.DeliveryNo}",
                    CreatedBy = currentUserId
                }, tx, cancellationToken);

                // Sync Financial Transaction
                var journalDto = new FinancialTransactionCreateDto
                {
                    TransactionDate = DateOnly.FromDateTime(dto.OrderDate),
                    TransactionType = "FREIGHT_VCB",
                    ReferenceType = "VcbDelivery",
                    ReferenceId = newId,
                    Description = $"ชำระค่าขนส่ง VCANBUY เลขที่ {dto.DeliveryNo}",
                    TotalAmount = dto.TransferredAmount,
                    PaymentMethod = "TRANSFER",
                    AttachmentUrl = transferSlipUrl,
                    Status = "POSTED",
                    BranchId = 1,
                    PostedAt = DateTime.Now,
                    PostedBy = currentUserId,
                    DocumentNo = dto.DeliveryNo,
                    PartnerName = "VCANBUY",
                    SlipDateTime = dto.OrderDate,
                    CreatedBy = currentUserId,
                    LedgerEntries = new List<LedgerEntryCreateDto>
                    {
                        new() { AccountId = 14, DebitAmount = dto.TransferredAmount, CreditAmount = 0, Memo = $"ชำระค่าขนส่ง VCANBUY เลขที่ {dto.DeliveryNo}" },
                        new() { AccountId = 2, DebitAmount = 0, CreditAmount = dto.TransferredAmount, Memo = $"ชำระค่าขนส่ง VCANBUY เลขที่ {dto.DeliveryNo}" }
                    }
                };

                await financialTransactionService.CreateTransactionWithLedgerAsync(journalDto, tx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return newId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating VcbDelivery DeliveryNo={DeliveryNo}", dto.DeliveryNo);
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateAsync(int id, VcbDeliveryCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        string? transferSlipUrl = null;

        if (slipFile != null && slipFile.Length > 0)
        {
            if (slipFile.Length > 5 * 1024 * 1024) throw new ArgumentException("ขนาดไฟล์ต้องไม่เกิน 5MB");
            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext)) throw new ArgumentException("รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");

            string uploadDir = Path.Combine(env.WebRootPath, "uploads", "slips", "deliveries");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string fileName = $"slip_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await slipFile.CopyToAsync(stream, cancellationToken);
            }

            transferSlipUrl = $"/uploads/slips/deliveries/{fileName}";
        }

        if (dto.OrderIds == null || !dto.OrderIds.Any())
            return ResultDto<bool>.Failure("ต้องเลือกออเดอร์อย่างน้อย 1 รายการ");

        if (dto.Items == null || !dto.Items.Any())
            return ResultDto<bool>.Failure("ต้องระบุข้อมูลกล่องสินค้าอย่างน้อย 1 กล่อง");

        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            bool updated = await repository.UpdateAsync(id, dto, transferSlipUrl, currentUserId, tx, cancellationToken);
            if (!updated)
            {
                await tx.RollbackAsync(cancellationToken);
                return ResultDto<bool>.Failure("ไม่พบข้อมูลใบส่งมอบ VCANBUY");
            }

            // Sync Expense (AccountTransaction)
            var existingExpenseId = await accountTransactionRepo.GetIdByReferenceAsync("VcbDeliveryCost", id, tx, cancellationToken);
            if (dto.TransferredAmount > 0)
            {
                if (existingExpenseId.HasValue)
                {
                    await accountTransactionRepo.UpdateAsync(existingExpenseId.Value, dto.TransferredAmount, $"ค่าขนส่ง VCANBUY บิล: {dto.DeliveryNo}", tx, cancellationToken);
                }
                else
                {
                    await accountTransactionRepo.CreateAsync(new Models.AccountTransactionModel
                    {
                        TransactionDate = DateTime.Now,
                        Type = "Expense",
                        Amount = dto.TransferredAmount,
                        ReferenceType = "VcbDeliveryCost",
                        ReferenceId = id,
                        Notes = $"ค่าขนส่ง VCANBUY บิล: {dto.DeliveryNo}",
                        CreatedBy = currentUserId
                    }, tx, cancellationToken);
                }
            }
            else if (existingExpenseId.HasValue)
            {
                await accountTransactionRepo.DeleteAsync(existingExpenseId.Value, tx, cancellationToken);
            }

            // Sync Financial Transaction
            var existingFinTxId = await financialTransactionService.GetTransactionIdByReferenceAsync("VcbDelivery", id, tx, cancellationToken);
            if (dto.TransferredAmount > 0)
            {
                var journalDto = new FinancialTransactionCreateDto
                {
                    TransactionDate = DateOnly.FromDateTime(dto.OrderDate),
                    TransactionType = "FREIGHT_VCB",
                    ReferenceType = "VcbDelivery",
                    ReferenceId = id,
                    Description = $"ชำระค่าขนส่ง VCANBUY เลขที่ {dto.DeliveryNo}",
                    TotalAmount = dto.TransferredAmount,
                    PaymentMethod = "TRANSFER",
                    AttachmentUrl = transferSlipUrl,
                    Status = "POSTED",
                    BranchId = 1,
                    PostedAt = DateTime.Now,
                    PostedBy = currentUserId,
                    DocumentNo = dto.DeliveryNo,
                    PartnerName = "VCANBUY",
                    SlipDateTime = dto.OrderDate,
                    CreatedBy = currentUserId,
                    LedgerEntries = new List<LedgerEntryCreateDto>
                    {
                        new() { AccountId = 14, DebitAmount = dto.TransferredAmount, CreditAmount = 0, Memo = $"ชำระค่าขนส่ง VCANBUY เลขที่ {dto.DeliveryNo}" },
                        new() { AccountId = 2, DebitAmount = 0, CreditAmount = dto.TransferredAmount, Memo = $"ชำระค่าขนส่ง VCANBUY เลขที่ {dto.DeliveryNo}" }
                    }
                };

                if (existingFinTxId.HasValue)
                {
                    await financialTransactionService.UpdateTransactionWithLedgerAsync(existingFinTxId.Value, journalDto, tx, cancellationToken);
                }
                else
                {
                    await financialTransactionService.CreateTransactionWithLedgerAsync(journalDto, tx, cancellationToken);
                }
            }
            else if (existingFinTxId.HasValue)
            {
                await financialTransactionService.DeleteTransactionWithLedgerAsync(existingFinTxId.Value, tx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return ResultDto<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating VcbDelivery Id={Id}", id);
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        var result = await repository.UpdateStatusAsync(id, status, currentUserId, null, cancellationToken);
        if (!result) return ResultDto<bool>.Failure("ไม่พบข้อมูลใบจัดส่ง VCANBUY");
        return ResultDto<bool>.Success(true);
    }
}

