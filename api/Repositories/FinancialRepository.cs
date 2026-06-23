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
                   ft.SlipDateTime, ft.OriginBank, ft.DestinationBank, ft.SourceAccountNo, ft.DestinationAccountNo, ft.SourceAccountName, ft.DestinationAccountName
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
                   ft.SlipDateTime, ft.OriginBank, ft.DestinationBank, ft.SourceAccountNo, ft.DestinationAccountNo, ft.SourceAccountName, ft.DestinationAccountName
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
                     SlipDateTime, OriginBank, DestinationBank, SourceAccountNo, DestinationAccountNo,
                     SourceAccountName, DestinationAccountName)
                OUTPUT INSERTED.TransactionId
                VALUES (@TransactionDate, @TransactionType, @ReferenceType, @ReferenceId, @TaxInvoiceId,
                        @Description, @TotalAmount, @PaymentMethod, @SourceAccountInfo, @PaymentRefNo,
                        @ReceiverAccountId, @AttachmentUrl, @ReceivedBy, @CreatedBy, GETDATE(), GETDATE(),
                        @Status, @BranchId, @PostedAt, @PostedBy, @VoidedAt, @VoidedBy, @DocumentNo, @PartnerName,
                        @SlipDateTime, @OriginBank, @DestinationBank, @SourceAccountNo, @DestinationAccountNo,
                        @SourceAccountName, @DestinationAccountName);";

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

    public async Task<ResultDto<int>> UpdateTransactionWithLedgerAsync(int transactionId, FinancialTransactionCreateDto dto)
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
            // Check if exists
            int exists = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.FinancialTransactions WHERE TransactionId = @Id", new { Id = transactionId }, transaction);
            if (exists == 0) return ResultDto<int>.Failure("ไม่พบข้อมูลรายการบัญชี");

            // Update status and audit fields
            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                dto.Status = "POSTED";
            }

            const string txSql = @"
                UPDATE dbo.FinancialTransactions SET
                     TransactionDate = @TransactionDate, 
                     TransactionType = @TransactionType, 
                     ReferenceType = @ReferenceType, 
                     ReferenceId = @ReferenceId, 
                     TaxInvoiceId = @TaxInvoiceId,
                     Description = @Description, 
                     TotalAmount = @TotalAmount, 
                     PaymentMethod = @PaymentMethod, 
                     SourceAccountInfo = @SourceAccountInfo, 
                     PaymentRefNo = @PaymentRefNo,
                     ReceiverAccountId = @ReceiverAccountId, 
                     AttachmentUrl = ISNULL(@AttachmentUrl, AttachmentUrl), 
                     UpdatedAt = GETDATE(),
                     Status = @Status, 
                     BranchId = @BranchId, 
                     DocumentNo = ISNULL(@DocumentNo, DocumentNo), 
                     PartnerName = @PartnerName,
                     SlipDateTime = @SlipDateTime, 
                     OriginBank = @OriginBank, 
                     DestinationBank = @DestinationBank,
                     SourceAccountNo = @SourceAccountNo,
                     DestinationAccountNo = @DestinationAccountNo,
                     SourceAccountName = @SourceAccountName,
                     DestinationAccountName = @DestinationAccountName
                WHERE TransactionId = @TransactionId;";

            var param = new DynamicParameters(dto);
            param.Add("TransactionId", transactionId);

            await connection.ExecuteAsync(txSql, param, transaction);

            // Delete old ledger entries
            await connection.ExecuteAsync("DELETE FROM dbo.FinancialLedgerEntries WHERE TransactionId = @TxId", new { TxId = transactionId }, transaction);

            // Insert new ledger entries in batch
            var batchSql = new StringBuilder();
            var batchParams = new DynamicParameters();
            batchParams.Add("TxId", transactionId);

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

            if (dto.LedgerEntries.Count > 0)
            {
                await connection.ExecuteAsync(batchSql.ToString(), batchParams, transaction);
            }

            await transaction.CommitAsync();
            return ResultDto<int>.Success(transactionId);
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

    public async Task<IEnumerable<GeneralJournalRowDto>> GetGeneralJournalAsync(DateOnly? dateFrom, DateOnly? dateTo)
    {
        using var conn = _connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var whereClause = new StringBuilder("WHERE 1=1");
        if (dateFrom.HasValue) { whereClause.Append(" AND ft.TransactionDate >= @DateFrom"); p.Add("DateFrom", dateFrom.Value); }
        if (dateTo.HasValue) { whereClause.Append(" AND ft.TransactionDate <= @DateTo"); p.Add("DateTo", dateTo.Value); }

        string sql = $@"
            SELECT le.EntryId, le.TransactionId, ft.TransactionDate, ft.DocumentNo, ft.TransactionType, le.Memo,
                   le.AccountId, ca.AccountCode, ca.AccountName, le.DebitAmount, le.CreditAmount
            FROM dbo.FinancialLedgerEntries le
            INNER JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            INNER JOIN dbo.ChartOfAccounts ca ON le.AccountId = ca.AccountId
            {whereClause}
            ORDER BY ft.TransactionDate ASC, ft.TransactionId ASC, le.EntryId ASC";

        return await conn.QueryAsync<GeneralJournalRowDto>(sql, p);
    }

    public async Task<IEnumerable<GeneralLedgerRowDto>> GetGeneralLedgerAsync(int accountId, DateOnly? dateFrom, DateOnly? dateTo)
    {
        using var conn = _connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("AccountId", accountId);

        // 1. Calculate Brought Forward
        var bfWhere = new StringBuilder("WHERE le.AccountId = @AccountId");
        if (dateFrom.HasValue) { bfWhere.Append(" AND ft.TransactionDate < @DateFrom"); p.Add("DateFrom", dateFrom.Value); }
        else { bfWhere.Append(" AND 1=0"); } // No brought forward if no start date

        string bfSql = $@"
            SELECT ISNULL(SUM(le.DebitAmount), 0) - ISNULL(SUM(le.CreditAmount), 0)
            FROM dbo.FinancialLedgerEntries le
            INNER JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            {bfWhere}";

        decimal bfBalance = await conn.ExecuteScalarAsync<decimal>(bfSql, p);

        // 2. Get Ledger Entries
        var txWhere = new StringBuilder("WHERE le.AccountId = @AccountId");
        if (dateFrom.HasValue) { txWhere.Append(" AND ft.TransactionDate >= @DateFrom"); }
        if (dateTo.HasValue) { txWhere.Append(" AND ft.TransactionDate <= @DateTo"); p.Add("DateTo", dateTo.Value); }

        string sql = $@"
            SELECT ft.TransactionDate, ft.DocumentNo, le.Memo, le.DebitAmount, le.CreditAmount
            FROM dbo.FinancialLedgerEntries le
            INNER JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            {txWhere}
            ORDER BY ft.TransactionDate ASC, ft.TransactionId ASC, le.EntryId ASC";

        var entries = (await conn.QueryAsync<GeneralLedgerRowDto>(sql, p)).ToList();

        // 3. Process running balance
        var result = new List<GeneralLedgerRowDto>();
        if (dateFrom.HasValue)
        {
            result.Add(new GeneralLedgerRowDto
            {
                TransactionDate = dateFrom.Value.AddDays(-1),
                Memo = "ยอดยกมา (Brought Forward)",
                DebitAmount = bfBalance > 0 ? bfBalance : 0,
                CreditAmount = bfBalance < 0 ? Math.Abs(bfBalance) : 0,
                RunningBalance = bfBalance,
                IsBroughtForward = true
            });
        }

        decimal currentBalance = bfBalance;
        foreach (var entry in entries)
        {
            currentBalance += (entry.DebitAmount - entry.CreditAmount);
            entry.RunningBalance = currentBalance;
            result.Add(entry);
        }

        return result;
    }

    public async Task<ProfitAndLossReportDto> GetProfitAndLossAsync(DateOnly? dateFrom, DateOnly? dateTo)
    {
        using var conn = _connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var txWhere = new StringBuilder("WHERE ca.AccountType IN ('REVENUE', 'EXPENSE')");
        if (dateFrom.HasValue) { txWhere.Append(" AND ft.TransactionDate >= @DateFrom"); p.Add("DateFrom", dateFrom.Value); }
        if (dateTo.HasValue) { txWhere.Append(" AND ft.TransactionDate <= @DateTo"); p.Add("DateTo", dateTo.Value); }

        string sql = $@"
            SELECT ca.AccountId, ca.AccountCode, ca.AccountName, ca.AccountType,
                   ISNULL(SUM(le.DebitAmount), 0) AS TotalDebit,
                   ISNULL(SUM(le.CreditAmount), 0) AS TotalCredit
            FROM dbo.ChartOfAccounts ca
            LEFT JOIN dbo.FinancialLedgerEntries le ON ca.AccountId = le.AccountId
            LEFT JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            {txWhere}
            GROUP BY ca.AccountId, ca.AccountCode, ca.AccountName, ca.AccountType, ca.SortOrder
            ORDER BY ca.SortOrder, ca.AccountCode;";

        var rows = await conn.QueryAsync(sql, p);

        var report = new ProfitAndLossReportDto { DateFrom = dateFrom, DateTo = dateTo };

        foreach (var row in rows)
        {
            decimal totalDebit = row.TotalDebit;
            decimal totalCredit = row.TotalCredit;
            string accountType = row.AccountType;

            var line = new ProfitAndLossLineDto
            {
                AccountId = row.AccountId,
                AccountCode = row.AccountCode,
                AccountName = row.AccountName
            };

            if (accountType == "REVENUE")
            {
                // Normal balance for Revenue is Credit
                line.Balance = totalCredit - totalDebit;
                report.RevenueLines.Add(line);
                report.TotalRevenue += line.Balance;
            }
            else if (accountType == "EXPENSE")
            {
                // Normal balance for Expense is Debit
                line.Balance = totalDebit - totalCredit;
                report.ExpenseLines.Add(line);
                report.TotalExpense += line.Balance;
            }
        }

        report.NetProfit = report.TotalRevenue - report.TotalExpense;
        return report;
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateOnly asOfDate)
    {
        using var conn = _connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("AsOfDate", asOfDate);

        // 1. Get Assets, Liabilities, Equity
        string sql = @"
            SELECT ca.AccountId, ca.AccountCode, ca.AccountName, ca.AccountType,
                   ISNULL(SUM(le.DebitAmount), 0) AS TotalDebit,
                   ISNULL(SUM(le.CreditAmount), 0) AS TotalCredit
            FROM dbo.ChartOfAccounts ca
            LEFT JOIN dbo.FinancialLedgerEntries le ON ca.AccountId = le.AccountId
            LEFT JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId AND ft.TransactionDate <= @AsOfDate
            WHERE ca.AccountType IN ('ASSET', 'LIABILITY', 'EQUITY')
            GROUP BY ca.AccountId, ca.AccountCode, ca.AccountName, ca.AccountType, ca.SortOrder
            ORDER BY ca.SortOrder, ca.AccountCode;";

        var rows = await conn.QueryAsync(sql, p);

        var report = new BalanceSheetReportDto { AsOfDate = asOfDate };

        foreach (var row in rows)
        {
            decimal totalDebit = row.TotalDebit;
            decimal totalCredit = row.TotalCredit;
            string accountType = row.AccountType;

            var line = new BalanceSheetLineDto
            {
                AccountId = row.AccountId,
                AccountCode = row.AccountCode,
                AccountName = row.AccountName
            };

            if (accountType == "ASSET")
            {
                // Normal balance for Asset is Debit
                line.Balance = totalDebit - totalCredit;
                report.AssetLines.Add(line);
                report.TotalAssets += line.Balance;
            }
            else if (accountType == "LIABILITY")
            {
                // Normal balance for Liability is Credit
                line.Balance = totalCredit - totalDebit;
                report.LiabilityLines.Add(line);
                report.TotalLiabilities += line.Balance;
            }
            else if (accountType == "EQUITY")
            {
                // Normal balance for Equity is Credit
                line.Balance = totalCredit - totalDebit;
                report.EquityLines.Add(line);
                report.TotalEquity += line.Balance;
            }
        }

        // 2. Calculate Retained Earnings (Net Profit up to AsOfDate)
        string plSql = @"
            SELECT ca.AccountType,
                   ISNULL(SUM(le.DebitAmount), 0) AS TotalDebit,
                   ISNULL(SUM(le.CreditAmount), 0) AS TotalCredit
            FROM dbo.ChartOfAccounts ca
            INNER JOIN dbo.FinancialLedgerEntries le ON ca.AccountId = le.AccountId
            INNER JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            WHERE ca.AccountType IN ('REVENUE', 'EXPENSE') AND ft.TransactionDate <= @AsOfDate
            GROUP BY ca.AccountType;";

        var plRows = await conn.QueryAsync(plSql, p);
        decimal totalRev = 0;
        decimal totalExp = 0;

        foreach (var r in plRows)
        {
            if (r.AccountType == "REVENUE") totalRev += ((decimal)r.TotalCredit - (decimal)r.TotalDebit);
            if (r.AccountType == "EXPENSE") totalExp += ((decimal)r.TotalDebit - (decimal)r.TotalCredit);
        }

        decimal retainedEarnings = totalRev - totalExp;
        if (retainedEarnings != 0)
        {
            report.EquityLines.Add(new BalanceSheetLineDto
            {
                AccountId = 0,
                AccountCode = "-",
                AccountName = "กำไร(ขาดทุน)สะสม (Retained Earnings)",
                Balance = retainedEarnings
            });
            report.TotalEquity += retainedEarnings;
        }

        report.TotalLiabilitiesAndEquity = report.TotalLiabilities + report.TotalEquity;
        return report;
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
