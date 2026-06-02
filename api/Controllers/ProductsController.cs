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

        int newProductId = await productService.CreateProductAsync(dto);

        return CreatedAtAction(nameof(CreateProduct),
            new { id = newProductId },
            new
            {
                message = "สร้างสินค้าสำเร็จ",
                productId = newProductId
            });
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchParams searchParams)
    {
        var result = await productService.GetProductsAsync(searchParams);
        return Ok(result);
    }
}
