using Dapper;
using PCS_API.DTOs;
using System.Data;
using System.Text;

namespace PCS_API.Repositories;

public class FinancialTransactionRepository(ISqlConnectionFactory connectionFactory) : IFinancialTransactionRepository
{
    public async Task<PagedResultDto<FinancialTransactionDto>> GetTransactionsPagedAsync(FinancialTransactionSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
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

    public async Task<FinancialTransactionDto?> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
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

        var cmd = new CommandDefinition(sql, new { transactionId }, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        var tx = await multi.ReadFirstOrDefaultAsync<FinancialTransactionDto>();
        if (tx == null) return null;
        tx.LedgerEntries = (await multi.ReadAsync<FinancialLedgerEntryDto>()).ToList();
        return tx;
    }

    public async Task<int> GetDailyTransactionCountAsync(DateOnly date, IDbTransaction tx, CancellationToken cancellationToken = default)
    {
        const string seqSql = "SELECT COUNT(*) FROM dbo.FinancialTransactions WHERE TransactionDate = @date";
        return await tx.Connection.ExecuteScalarAsync<int>(new CommandDefinition(seqSql, new { date }, transaction: tx, cancellationToken: cancellationToken));
    }

    public async Task<int> InsertTransactionAsync(FinancialTransactionCreateDto dto, IDbTransaction tx, CancellationToken cancellationToken = default)
    {
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

        return await tx.Connection.QuerySingleAsync<int>(new CommandDefinition(txSql, dto, transaction: tx, cancellationToken: cancellationToken));
    }

    public async Task InsertLedgerEntriesAsync(int transactionId, List<LedgerEntryCreateDto> entries, IDbTransaction tx, CancellationToken cancellationToken = default)
    {
        var ledgerData = entries.Select(e => new {
            TxId = transactionId,
            AccId = e.AccountId,
            Dbt = e.DebitAmount,
            Crd = e.CreditAmount,
            Memo = e.Memo
        }).ToList();

        if (ledgerData.Count > 0)
        {
            const string batchSql = @"
                INSERT INTO dbo.FinancialLedgerEntries
                    (TransactionId, AccountId, DebitAmount, CreditAmount, Memo)
                VALUES (@TxId, @AccId, @Dbt, @Crd, @Memo);";
            await tx.Connection.ExecuteAsync(new CommandDefinition(batchSql, ledgerData, transaction: tx, cancellationToken: cancellationToken));
        }
    }

    public async Task<int> CheckTransactionExistsAsync(int transactionId, IDbTransaction tx, CancellationToken cancellationToken = default)
    {
        return await tx.Connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM dbo.FinancialTransactions WHERE TransactionId = @Id", new { Id = transactionId }, transaction: tx, cancellationToken: cancellationToken));
    }

    public async Task UpdateTransactionAsync(int transactionId, FinancialTransactionCreateDto dto, IDbTransaction tx, CancellationToken cancellationToken = default)
    {
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

        await tx.Connection.ExecuteAsync(new CommandDefinition(txSql, param, transaction: tx, cancellationToken: cancellationToken));
    }

    public async Task DeleteLedgerEntriesAsync(int transactionId, IDbTransaction tx, CancellationToken cancellationToken = default)
    {
        await tx.Connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.FinancialLedgerEntries WHERE TransactionId = @TxId", new { TxId = transactionId }, transaction: tx, cancellationToken: cancellationToken));
    }
}
