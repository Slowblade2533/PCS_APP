using Dapper;
using PCS_API.DTOs;
using System.Text;

namespace PCS_API.Repositories;

public class TaxInvoiceRepository(ISqlConnectionFactory connectionFactory) : ITaxInvoiceRepository
{
    public async Task<PagedResultDto<TaxInvoiceDto>> GetTaxInvoicesPagedAsync(TaxInvoiceSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
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

    public async Task<TaxInvoiceDto?> GetTaxInvoiceByIdAsync(int taxInvoiceId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TaxInvoiceId, TaxInvoiceNo, TaxInvoiceDate, TaxType,
                   PartnerName, PartnerTaxId, PartnerAddress,
                   BaseAmount, VatRate, VatAmount, TotalAmount, DocumentUrl, Notes, CreatedAt
            FROM dbo.TaxInvoices WHERE TaxInvoiceId = @taxInvoiceId;";
        return await conn.QueryFirstOrDefaultAsync<TaxInvoiceDto>(new CommandDefinition(sql, new { taxInvoiceId }, cancellationToken: cancellationToken));
    }

    public async Task<int> InsertTaxInvoiceAsync(TaxInvoiceCreateDto dto, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.TaxInvoices
                (TaxInvoiceNo, TaxInvoiceDate, TaxType, PartnerName, PartnerTaxId, PartnerAddress,
                 BaseAmount, VatRate, VatAmount, TotalAmount, DocumentUrl, Notes, CreatedBy)
            OUTPUT INSERTED.TaxInvoiceId
            VALUES (@TaxInvoiceNo, @TaxInvoiceDate, @TaxType, @PartnerName, @PartnerTaxId, @PartnerAddress,
                    @BaseAmount, @VatRate, @VatAmount, @TotalAmount, @DocumentUrl, @Notes, @CreatedBy);";
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, dto, cancellationToken: cancellationToken));
    }
}
