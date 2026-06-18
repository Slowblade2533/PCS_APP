using Dapper;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Text;

namespace PCS_API.Repositories;

public class FinancialRepository : IFinancialRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public FinancialRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // ─── Chart of Accounts ────────────────────────────────────────────────────
    public async Task<IEnumerable<ChartOfAccountDto>> GetAllAccountsAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT AccountId, AccountCode, AccountName, AccountType, NormalBalance,
                   IsSystemAccount, Description, SortOrder, IsActive
            FROM dbo.ChartOfAccounts
            ORDER BY SortOrder, AccountCode;";
        return await conn.QueryAsync<ChartOfAccountDto>(sql);
    }

    public async Task<ChartOfAccountDto?> GetAccountByIdAsync(int accountId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT AccountId, AccountCode, AccountName, AccountType, NormalBalance,
                   IsSystemAccount, Description, SortOrder, IsActive
            FROM dbo.ChartOfAccounts WHERE AccountId = @accountId;";
        return await conn.QueryFirstOrDefaultAsync<ChartOfAccountDto>(sql, new { accountId });
    }

    public async Task<ResultDto<int>> CreateAccountAsync(ChartOfAccountCreateDto dto)
    {
        using var conn = _connectionFactory.CreateConnection();
        // Check duplicate code
        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.ChartOfAccounts WHERE AccountCode = @AccountCode;", dto);
        if (existing > 0)
            return ResultDto<int>.Failure($"รหัสบัญชี '{dto.AccountCode}' มีอยู่ในระบบแล้ว");

        const string sql = @"
            INSERT INTO dbo.ChartOfAccounts
                (AccountCode, AccountName, AccountType, NormalBalance, Description, SortOrder)
            OUTPUT INSERTED.AccountId
            VALUES (@AccountCode, @AccountName, @AccountType, @NormalBalance, @Description, @SortOrder);";
        int newId = await conn.QuerySingleAsync<int>(sql, dto);
        return ResultDto<int>.Success(newId);
    }

    // ─── Tax Invoices ─────────────────────────────────────────────────────────
    public async Task<PagedResultDto<TaxInvoiceDto>> GetTaxInvoicesPagedAsync(
        TaxInvoiceSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var where = new StringBuilder("WHERE 1=1");

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            where.Append(" AND (TaxInvoiceNo LIKE @Search OR PartnerName LIKE @Search OR PartnerTaxId LIKE @Search)");
            p.Add("Search", $"%{search.SearchTerm}%");
        }
        if (!string.IsNullOrEmpty(search.TaxType))
        {
            where.Append(" AND TaxType = @TaxType");
            p.Add("TaxType", search.TaxType);
        }
        if (search.DateFrom.HasValue) { where.Append(" AND TaxInvoiceDate >= @DateFrom"); p.Add("DateFrom", search.DateFrom.Value); }
        if (search.DateTo.HasValue) { where.Append(" AND TaxInvoiceDate <= @DateTo"); p.Add("DateTo", search.DateTo.Value); }

        p.Add("Offset", search.GetSafeOffset());
        p.Add("PageSize", search.PageSize);

        string sql = $@"
            SELECT COUNT(*) FROM dbo.TaxInvoices {where};
            SELECT TaxInvoiceId, TaxInvoiceNo, TaxInvoiceDate, TaxType,
                   PartnerName, PartnerTaxId, PartnerAddress,
                   BaseAmount, VatRate, VatAmount, TotalAmount, DocumentUrl, Notes, CreatedAt
            FROM dbo.TaxInvoices {where}
            ORDER BY TaxInvoiceDate DESC, TaxInvoiceId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var cmd = new CommandDefinition(sql, p, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        int total = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<TaxInvoiceDto>();

        return new PagedResultDto<TaxInvoiceDto>
        {
            Items = items, TotalCount = total,
            PageNumber = search.PageNumber, PageSize = search.PageSize
        };
    }

    public async Task<TaxInvoiceDto?> GetTaxInvoiceByIdAsync(int taxInvoiceId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TaxInvoiceId, TaxInvoiceNo, TaxInvoiceDate, TaxType,
                   PartnerName, PartnerTaxId, PartnerAddress,
                   BaseAmount, VatRate, VatAmount, TotalAmount, DocumentUrl, Notes, CreatedAt
            FROM dbo.TaxInvoices WHERE TaxInvoiceId = @taxInvoiceId;";
        return await conn.QueryFirstOrDefaultAsync<TaxInvoiceDto>(sql, new { taxInvoiceId });
    }

    public async Task<ResultDto<int>> CreateTaxInvoiceAsync(TaxInvoiceCreateDto dto)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.TaxInvoices
                (TaxInvoiceNo, TaxInvoiceDate, TaxType, PartnerName, PartnerTaxId, PartnerAddress,
                 BaseAmount, VatRate, VatAmount, TotalAmount, DocumentUrl, Notes, CreatedBy)
            OUTPUT INSERTED.TaxInvoiceId
            VALUES (@TaxInvoiceNo, @TaxInvoiceDate, @TaxType, @PartnerName, @PartnerTaxId, @PartnerAddress,
                    @BaseAmount, @VatRate, @VatAmount, @TotalAmount, @DocumentUrl, @Notes, @CreatedBy);";
        int newId = await conn.QuerySingleAsync<int>(sql, dto);
        return ResultDto<int>.Success(newId);
    }

    // ─── Financial Transactions ────────────────────────────────────────────────
    public async Task<PagedResultDto<FinancialTransactionDto>> GetTransactionsPagedAsync(
        FinancialTransactionSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var where = new StringBuilder("WHERE 1=1");

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            where.Append(" AND (ft.Description LIKE @Search OR ft.PaymentRefNo LIKE @Search OR ft.ReferenceType LIKE @Search)");
            p.Add("Search", $"%{search.SearchTerm}%");
        }
        if (!string.IsNullOrEmpty(search.TransactionType)) { where.Append(" AND ft.TransactionType = @TransactionType"); p.Add("TransactionType", search.TransactionType); }
        if (search.DateFrom.HasValue) { where.Append(" AND ft.TransactionDate >= @DateFrom"); p.Add("DateFrom", search.DateFrom.Value); }
        if (search.DateTo.HasValue) { where.Append(" AND ft.TransactionDate <= @DateTo"); p.Add("DateTo", search.DateTo.Value); }

        p.Add("Offset", search.GetSafeOffset());
        p.Add("PageSize", search.PageSize);

        string sql = $@"
            SELECT COUNT(*) FROM dbo.FinancialTransactions ft {where};
            SELECT ft.TransactionId, ft.TransactionDate, ft.TransactionType, ft.ReferenceType,
                   ft.ReferenceId, ft.Description, ft.TotalAmount, ft.PaymentMethod,
                   ft.PaymentRefNo, ft.SourceAccountInfo, ft.AttachmentUrl,
                   ca.AccountName AS ReceiverAccountName,
                   u.Username AS ReceivedByName, ft.CreatedAt,
                   ft.Status, ft.BranchId, b.BranchName, ft.PostedAt, ft.PostedBy,
                   u2.Username AS PostedByName, ft.VoidedAt, ft.VoidedBy,
                   u3.Username AS VoidedByName, ft.DocumentNo, ft.PartnerName,
                   ft.SlipDateTime, ft.OriginBank, ft.DestinationBank
            FROM dbo.FinancialTransactions ft
            LEFT JOIN dbo.ChartOfAccounts ca ON ft.ReceiverAccountId = ca.AccountId
            LEFT JOIN dbo.Users u ON ft.ReceivedBy = u.Id
            LEFT JOIN dbo.Branches b ON ft.BranchId = b.Id
            LEFT JOIN dbo.Users u2 ON ft.PostedBy = u2.Id
            LEFT JOIN dbo.Users u3 ON ft.VoidedBy = u3.Id
            {where}
            ORDER BY ft.TransactionDate DESC, ft.TransactionId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var cmd = new CommandDefinition(sql, p, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        int total = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<FinancialTransactionDto>();

        return new PagedResultDto<FinancialTransactionDto>
        {
            Items = items, TotalCount = total,
            PageNumber = search.PageNumber, PageSize = search.PageSize
        };
    }

    public async Task<FinancialTransactionDto?> GetTransactionByIdAsync(int transactionId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ft.TransactionId, ft.TransactionDate, ft.TransactionType, ft.ReferenceType,
                   ft.ReferenceId, ft.Description, ft.TotalAmount, ft.PaymentMethod,
                   ft.PaymentRefNo, ft.SourceAccountInfo, ft.AttachmentUrl,
                   ca.AccountName AS ReceiverAccountName,
                   u.Username AS ReceivedByName, ft.CreatedAt,
                   ft.Status, ft.BranchId, b.BranchName, ft.PostedAt, ft.PostedBy,
                   u2.Username AS PostedByName, ft.VoidedAt, ft.VoidedBy,
                   u3.Username AS VoidedByName, ft.DocumentNo, ft.PartnerName,
                   ft.SlipDateTime, ft.OriginBank, ft.DestinationBank
            FROM dbo.FinancialTransactions ft
            LEFT JOIN dbo.ChartOfAccounts ca ON ft.ReceiverAccountId = ca.AccountId
            LEFT JOIN dbo.Users u ON ft.ReceivedBy = u.Id
            LEFT JOIN dbo.Branches b ON ft.BranchId = b.Id
            LEFT JOIN dbo.Users u2 ON ft.PostedBy = u2.Id
            LEFT JOIN dbo.Users u3 ON ft.VoidedBy = u3.Id
            WHERE ft.TransactionId = @transactionId;

            SELECT le.EntryId, le.AccountId, ca.AccountCode, ca.AccountName,
                   le.DebitAmount, le.CreditAmount, le.Memo
            FROM dbo.FinancialLedgerEntries le
            INNER JOIN dbo.ChartOfAccounts ca ON le.AccountId = ca.AccountId
            WHERE le.TransactionId = @transactionId
            ORDER BY le.EntryId;";

        using var multi = await conn.QueryMultipleAsync(sql, new { transactionId });
        var tx = await multi.ReadFirstOrDefaultAsync<FinancialTransactionDto>();
        if (tx == null) return null;
        tx.LedgerEntries = (await multi.ReadAsync<FinancialLedgerEntryDto>()).ToList();
        return tx;
    }

    public async Task<ResultDto<int>> CreateTransactionWithLedgerAsync(FinancialTransactionCreateDto dto)
    {
        // Validate: sum of debits must equal sum of credits
        var totalDebit = dto.LedgerEntries.Sum(e => e.DebitAmount);
        var totalCredit = dto.LedgerEntries.Sum(e => e.CreditAmount);
        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            return ResultDto<int>.Failure($"ยอด Debit ({totalDebit:N2}) ต้องเท่ากับ Credit ({totalCredit:N2}) เสมอ");

        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Auto-generate DocumentNo if not provided
            if (string.IsNullOrWhiteSpace(dto.DocumentNo))
            {
                const string seqSql = "SELECT COUNT(*) + 1 FROM dbo.FinancialTransactions WHERE TransactionDate = @TransactionDate";
                int count = await connection.ExecuteScalarAsync<int>(seqSql, new { TransactionDate = dto.TransactionDate }, transaction);
                dto.DocumentNo = $"TX-{dto.TransactionDate:yyyyMMdd}-{count:D4}";
            }

            // Set default status and audit fields
            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                dto.Status = "POSTED";
            }
            if (dto.Status == "POSTED")
            {
                dto.PostedAt ??= DateTime.UtcNow;
                dto.PostedBy ??= dto.CreatedBy;
            }
            else if (dto.Status == "VOID")
            {
                dto.VoidedAt ??= DateTime.UtcNow;
                dto.VoidedBy ??= dto.CreatedBy;
            }

            const string txSql = @"
                INSERT INTO dbo.FinancialTransactions
                    (TransactionDate, TransactionType, ReferenceType, ReferenceId, TaxInvoiceId,
                     Description, TotalAmount, PaymentMethod, SourceAccountInfo, PaymentRefNo,
                     ReceiverAccountId, AttachmentUrl, ReceivedBy, CreatedBy, CreatedAt, UpdatedAt,
                     Status, BranchId, PostedAt, PostedBy, VoidedAt, VoidedBy, DocumentNo, PartnerName,
                     SlipDateTime, OriginBank, DestinationBank)
                OUTPUT INSERTED.TransactionId
                VALUES (@TransactionDate, @TransactionType, @ReferenceType, @ReferenceId, @TaxInvoiceId,
                        @Description, @TotalAmount, @PaymentMethod, @SourceAccountInfo, @PaymentRefNo,
                        @ReceiverAccountId, @AttachmentUrl, @ReceivedBy, @CreatedBy, GETDATE(), GETDATE(),
                        @Status, @BranchId, @PostedAt, @PostedBy, @VoidedAt, @VoidedBy, @DocumentNo, @PartnerName,
                        @SlipDateTime, @OriginBank, @DestinationBank);";

            int txId = await connection.QuerySingleAsync<int>(txSql, dto, transaction);

            // Insert ledger entries in batch
            var batchSql = new StringBuilder();
            var batchParams = new DynamicParameters();
            batchParams.Add("TxId", txId);

            for (int i = 0; i < dto.LedgerEntries.Count; i++)
            {
                var e = dto.LedgerEntries[i];
                batchSql.AppendLine($@"
                    INSERT INTO dbo.FinancialLedgerEntries
                        (TransactionId, AccountId, DebitAmount, CreditAmount, Memo)
                    VALUES (@TxId, @AccId{i}, @Dbt{i}, @Crd{i}, @Memo{i});");
                batchParams.Add($"AccId{i}", e.AccountId);
                batchParams.Add($"Dbt{i}", e.DebitAmount);
                batchParams.Add($"Crd{i}", e.CreditAmount);
                batchParams.Add($"Memo{i}", e.Memo);
            }

            await connection.ExecuteAsync(batchSql.ToString(), batchParams, transaction);
            await transaction.CommitAsync();
            return ResultDto<int>.Success(txId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ─── Trial Balance ────────────────────────────────────────────────────────
    public async Task<IEnumerable<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? dateFrom, DateOnly? dateTo)
    {
        using var conn = _connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var txWhere = new StringBuilder("WHERE 1=1");
        if (dateFrom.HasValue) { txWhere.Append(" AND ft.TransactionDate >= @DateFrom"); p.Add("DateFrom", dateFrom.Value); }
        if (dateTo.HasValue) { txWhere.Append(" AND ft.TransactionDate <= @DateTo"); p.Add("DateTo", dateTo.Value); }

        string sql = $@"
            SELECT ca.AccountId, ca.AccountCode, ca.AccountName, ca.AccountType,
                   ISNULL(SUM(le.DebitAmount), 0) AS TotalDebit,
                   ISNULL(SUM(le.CreditAmount), 0) AS TotalCredit,
                   ISNULL(SUM(le.DebitAmount), 0) - ISNULL(SUM(le.CreditAmount), 0) AS Balance
            FROM dbo.ChartOfAccounts ca
            LEFT JOIN dbo.FinancialLedgerEntries le ON ca.AccountId = le.AccountId
            LEFT JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            {txWhere}
            GROUP BY ca.AccountId, ca.AccountCode, ca.AccountName, ca.AccountType, ca.SortOrder
            HAVING ISNULL(SUM(le.DebitAmount), 0) > 0 OR ISNULL(SUM(le.CreditAmount), 0) > 0
            ORDER BY ca.SortOrder, ca.AccountCode;";

        return await conn.QueryAsync<TrialBalanceRowDto>(sql, p);
    }

    public async Task<bool> IsAttachmentUsedAsync(string attachmentUrl)
    {
        if (string.IsNullOrWhiteSpace(attachmentUrl)) return false;
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM dbo.FinancialTransactions WHERE AttachmentUrl = @Url";
        int count = await conn.ExecuteScalarAsync<int>(sql, new { Url = attachmentUrl });
        return count > 0;
    }
}
