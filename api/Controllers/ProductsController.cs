using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            int newProductId = await productService.CreateProductAsync(dto);

            return CreatedAtAction(nameof(CreateProduct),
                new { id = newProductId },
                new
                {
                    message = "สร้างสินค้าสำเร็จ",
                    productId = newProductId
                });
        }
        catch (InvalidOperationException ex) // ✨ จับข้อความเตือนเรื่องค่าซ้ำจากการตรวจเช็ค
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchParams searchParams)
    {
        var result = await productService.GetProductsAsync(searchParams);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await productService.GetProductByIdAsync(id);
        if (product == null) return NotFound(new { message = "ไม่พบข้อมูลสินค้าที่ระบุ" });
        return Ok(product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            bool isUpdated = await productService.UpdateProductAsync(id, dto);
            if (!isUpdated) return NotFound(new { message = "ไม่พบสินค้าที่ต้องการแก้ไข" });

            return Ok(new { message = "แก้ไขข้อมูลสินค้าสำเร็จ" });
        }
        catch (InvalidOperationException ex) // ✨ จับข้อความเตือนเรื่องค่าซ้ำจากการตรวจเช็คในโหมดแก้ไข
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
