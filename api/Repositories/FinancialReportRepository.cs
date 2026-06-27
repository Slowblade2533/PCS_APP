using Dapper;
using PCS_API.DTOs;
using System.Text;

namespace PCS_API.Repositories;

public class FinancialReportRepository(ISqlConnectionFactory connectionFactory) : IFinancialReportRepository
{
    public async Task<IEnumerable<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var txWhere = new StringBuilder("WHERE 1=1");
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

        return await conn.QueryAsync<TrialBalanceRowDto>(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<GeneralJournalRowDto>> GetGeneralJournalAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
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

        return await conn.QueryAsync<GeneralJournalRowDto>(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    public async Task<decimal> GetBroughtForwardBalanceAsync(int accountId, DateOnly? dateFrom, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("AccountId", accountId);

        var bfWhere = new StringBuilder("WHERE le.AccountId = @AccountId");
        if (dateFrom.HasValue) { bfWhere.Append(" AND ft.TransactionDate < @DateFrom"); p.Add("DateFrom", dateFrom.Value); }
        else { bfWhere.Append(" AND 1=0"); }

        string bfSql = $@"
            SELECT ISNULL(SUM(le.DebitAmount), 0) - ISNULL(SUM(le.CreditAmount), 0)
            FROM dbo.FinancialLedgerEntries le
            INNER JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            {bfWhere}";

        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(bfSql, p, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<GeneralLedgerRowDto>> GetLedgerEntriesAsync(int accountId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("AccountId", accountId);

        var txWhere = new StringBuilder("WHERE le.AccountId = @AccountId");
        if (dateFrom.HasValue) { txWhere.Append(" AND ft.TransactionDate >= @DateFrom"); p.Add("DateFrom", dateFrom.Value); }
        if (dateTo.HasValue) { txWhere.Append(" AND ft.TransactionDate <= @DateTo"); p.Add("DateTo", dateTo.Value); }

        string sql = $@"
            SELECT ft.TransactionDate, ft.DocumentNo, le.Memo, le.DebitAmount, le.CreditAmount
            FROM dbo.FinancialLedgerEntries le
            INNER JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            {txWhere}
            ORDER BY ft.TransactionDate ASC, ft.TransactionId ASC, le.EntryId ASC";

        return await conn.QueryAsync<GeneralLedgerRowDto>(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<ReportAccountBalanceDto>> GetProfitAndLossBalancesAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
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

        return await conn.QueryAsync<ReportAccountBalanceDto>(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<ReportAccountBalanceDto>> GetBalanceSheetBalancesAsync(DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("AsOfDate", asOfDate);

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

        return await conn.QueryAsync<ReportAccountBalanceDto>(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<ReportAccountBalanceDto>> GetRetainedEarningsBalancesAsync(DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("AsOfDate", asOfDate);

        string sql = @"
            SELECT ca.AccountType,
                   ISNULL(SUM(le.DebitAmount), 0) AS TotalDebit,
                   ISNULL(SUM(le.CreditAmount), 0) AS TotalCredit
            FROM dbo.ChartOfAccounts ca
            INNER JOIN dbo.FinancialLedgerEntries le ON ca.AccountId = le.AccountId
            INNER JOIN dbo.FinancialTransactions ft ON le.TransactionId = ft.TransactionId
            WHERE ca.AccountType IN ('REVENUE', 'EXPENSE') AND ft.TransactionDate <= @AsOfDate
            GROUP BY ca.AccountType;";

        return await conn.QueryAsync<ReportAccountBalanceDto>(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    public async Task<bool> IsAttachmentUsedAsync(string attachmentUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(attachmentUrl)) return false;
        using var conn = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM dbo.FinancialTransactions WHERE AttachmentUrl = @Url";
        int count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Url = attachmentUrl }, cancellationToken: cancellationToken));
        return count > 0;
    }
}
