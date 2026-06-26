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
                throw new ArgumentException("เธเธเธฒเธ”เนเธเธฅเนเธ•เนเธญเธเนเธกเนเน€เธเธดเธ 5MB");
            }

            // Validate extension
            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
            {
                throw new ArgumentException("เธฃเธญเธเธฃเธฑเธเน€เธเธเธฒเธฐเนเธเธฅเนเธฃเธนเธเธ เธฒเธ JPG, PNG, WEBP เน€เธ—เนเธฒเธเธฑเนเธ");
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
                    Notes = $"เธเนเธฒเธเธเธชเนเธ VCANBUY เธเธดเธฅ: {dto.DeliveryNo}",
                    CreatedBy = currentUserId
                }, tx, cancellationToken);

                // Sync Financial Transaction
                var journalDto = new FinancialTransactionCreateDto
                {
                    TransactionDate = DateOnly.FromDateTime(dto.OrderDate),
                    TransactionType = "FREIGHT_VCB",
                    ReferenceType = "VcbDelivery",
                    ReferenceId = newId,
                    Description = $"เธเธณเธฃเธฐเธเนเธฒเธเธเธชเนเธ VCANBUY เน€เธฅเธเธ—เธตเน {dto.DeliveryNo}",
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
                        new() { AccountId = 14, DebitAmount = dto.TransferredAmount, CreditAmount = 0, Memo = $"เธเธณเธฃเธฐเธเนเธฒเธเธเธชเนเธ VCANBUY เน€เธฅเธเธ—เธตเน {dto.DeliveryNo}" },
                        new() { AccountId = 2, DebitAmount = 0, CreditAmount = dto.TransferredAmount, Memo = $"เธเธณเธฃเธฐเธเนเธฒเธเธเธชเนเธ VCANBUY เน€เธฅเธเธ—เธตเน {dto.DeliveryNo}" }
                    }
                };

                await financialTransactionService.CreateTransactionWithLedgerAsync(journalDto, tx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return newId;
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateAsync(int id, VcbDeliveryCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        string? transferSlipUrl = null;

        if (slipFile != null && slipFile.Length > 0)
        {
            if (slipFile.Length > 5 * 1024 * 1024) throw new ArgumentException("เธเธเธฒเธ”เนเธเธฅเนเธ•เนเธญเธเนเธกเนเน€เธเธดเธ 5MB");
            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext)) throw new ArgumentException("เธฃเธญเธเธฃเธฑเธเน€เธเธเธฒเธฐเนเธเธฅเนเธฃเธนเธเธ เธฒเธ JPG, PNG, WEBP เน€เธ—เนเธฒเธเธฑเนเธ");

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
            return ResultDto<bool>.Failure("เธ•เนเธญเธเน€เธฅเธทเธญเธเธญเธญเน€เธ”เธญเธฃเนเธญเธขเนเธฒเธเธเนเธญเธข 1 เธฃเธฒเธขเธเธฒเธฃ");

        if (dto.Items == null || !dto.Items.Any())
            return ResultDto<bool>.Failure("เธ•เนเธญเธเธฃเธฐเธเธธเธเนเธญเธกเธนเธฅเธเธฅเนเธญเธเธชเธดเธเธเนเธฒเธญเธขเนเธฒเธเธเนเธญเธข 1 เธเธฅเนเธญเธ");

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
                return ResultDto<bool>.Failure("เนเธกเนเธเธเธเนเธญเธกเธนเธฅเนเธเธชเนเธเธกเธญเธ VCANBUY");
            }

            // Sync Expense (AccountTransaction)
            var existingExpenseId = await accountTransactionRepo.GetIdByReferenceAsync("VcbDeliveryCost", id, tx, cancellationToken);
            if (dto.TransferredAmount > 0)
            {
                if (existingExpenseId.HasValue)
                {
                    await accountTransactionRepo.UpdateAsync(existingExpenseId.Value, dto.TransferredAmount, $"เธเนเธฒเธเธเธชเนเธ VCANBUY เธเธดเธฅ: {dto.DeliveryNo}", tx, cancellationToken);
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
                        Notes = $"เธเนเธฒเธเธเธชเนเธ VCANBUY เธเธดเธฅ: {dto.DeliveryNo}",
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
                    Description = $"เธเธณเธฃเธฐเธเนเธฒเธเธเธชเนเธ VCANBUY เน€เธฅเธเธ—เธตเน {dto.DeliveryNo}",
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
                        new() { AccountId = 14, DebitAmount = dto.TransferredAmount, CreditAmount = 0, Memo = $"เธเธณเธฃเธฐเธเนเธฒเธเธเธชเนเธ VCANBUY เน€เธฅเธเธ—เธตเน {dto.DeliveryNo}" },
                        new() { AccountId = 2, DebitAmount = 0, CreditAmount = dto.TransferredAmount, Memo = $"เธเธณเธฃเธฐเธเนเธฒเธเธเธชเนเธ VCANBUY เน€เธฅเธเธ—เธตเน {dto.DeliveryNo}" }
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
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        var result = await repository.UpdateStatusAsync(id, status, currentUserId, null, cancellationToken);
        if (!result) return ResultDto<bool>.Failure("เนเธกเนเธเธเธเนเธญเธกเธนเธฅเนเธเธเธฑเธ”เธชเนเธ VCANBUY");
        return ResultDto<bool>.Success(true);
    }
}

