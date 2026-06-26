using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet("search")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> SearchCategories([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllCategoriesAsync(q, cancellationToken);
        return Ok(categories);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var category = await categoryService.GetCategoryByIdAsync(id);
        if (category == null) 
            return NotFound(new { message = "ไม่พบข้อมูลหมวดหมู่" });

        return Ok(category);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageSettings")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await categoryService.CreateCategoryAsync(dto);
        if (!result.IsSuccess) 
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetCategoryById), new { id = result.CategoryId }, new
        {
            message = "สร้างหมวดหมู่สำเร็จ",
            categoryId = result.CategoryId
        });
    }

    [HttpPost("batch")]
    [Authorize(Policy = "CanManageSettings")]
    public async Task<IActionResult> CreateCategoryBatch([FromBody] CategoryBatchCreateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await categoryService.CreateCategoryBatchAsync(dto);
        if (!result.IsSuccess) 
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new
        {
            message = "สร้างชุดหมวดหมู่สำเร็จ",
            categoryId = result.CategoryId
        });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageSettings")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await categoryService.UpdateCategoryAsync(id, dto);
        if (!result.IsSuccess) 
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "แก้ไขหมวดหมู่สำเร็จ" });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageSettings")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await categoryService.DeleteCategoryAsync(id);
        if (!result.IsSuccess) 
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "ลบหมวดหมู่สำเร็จ" });
    }
}
