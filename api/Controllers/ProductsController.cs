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
    [Authorize(Policy = "CanCreateProduct")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await productService.CreateProductAsync(dto);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(CreateProduct),
            new { id = result.Value },
            new
            {
                message = "สร้างสินค้าสำเร็จ",
                productId = result.Value
            });
    }

    [HttpGet]
    [Authorize(Policy = "CanViewProduct")]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchParamsDto searchParams, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await productService.GetProductsAsync(searchParams, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewProduct")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await productService.GetProductByIdAsync(id);

        if (product == null)
        {
            return NotFound(new { message = "ไม่พบข้อมูลสินค้าที่ระบุ" });
        }

        return Ok(product);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanEditProduct")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await productService.UpdateProductAsync(id, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "แก้ไขข้อมูลสินค้าสำเร็จ" });
    }
}