using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "CanCreateProduct")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await productService.CreateProductWithVariantsAsync(dto, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(CreateProduct),
            new { id = result.Value },
            new
            {
                message = "เธชเธฃเนเธฒเธเธชเธดเธเธเนเธฒเธชเธณเน€เธฃเนเธ",
                productId = result.Value
            });
    }

    [HttpGet]
    [Authorize(Policy = "CanViewProduct")]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchParamsDto searchParams, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await productService.GetPagedProductsAsync(searchParams, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewProduct")]
    public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductDetailAsync(id, cancellationToken);
        if (product == null)
            return NotFound(new { message = "เนเธกเนเธเธเธเนเธญเธกเธนเธฅเธชเธดเธเธเนเธฒเธ—เธตเนเธฃเธฐเธเธธ" });


        return Ok(product);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanEditProduct")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await productService.UpdateProductWithVariantsAsync(id, dto, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "เนเธเนเนเธเธเนเธญเธกเธนเธฅเธชเธดเธเธเนเธฒเธชเธณเน€เธฃเนเธ" });
    }
}

