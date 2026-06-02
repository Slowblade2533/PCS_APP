namespace PCS_API.DTOs;

// รับข้อมูลการค้นหาจาก Frontend
public class ProductSearchParams
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? ProductStatus { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ข้อมูลสินค้าที่ Join แล้วสำหรับแสดงในตาราง List
public class ProductListDto
{
    public int ProductId { get; set; }
    public string ProductNameTh { get; set; } = null!;
    public string? ProductNameEn { get; set; }
    public string? BrandName { get; set; }
    public string CategoryName { get; set; } = null!;
    public string ProductType { get; set; } = null!;
    public string ProductStatus { get; set; } = null!;
    public int TotalVariants { get; set; }      // จำนวน SKU ย่อย
    public int TotalAvailableStock { get; set; } // ผลรวม AvailableQuantity จากตาราง Stocks
    public decimal MinPrice { get; set; }        // ราคาเริ่มต้นของสินค้านี้
    public int TotalCount { get; set; } // เพิ่มบรรทัดนี้เข้ามาเพื่อรองรับ SQL COUNT(*) OVER()
}

// แรปข้อมูลรวมยอดและรายการส่งกลับไปทำ Pagination ที่หน้าเว็บ
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}