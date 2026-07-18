using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    public class CategoriesController : ApiControllerBase
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<CategoryReadDto>>> GetAll()
        {
            var categories = await _db.Categories
                .Where(c => c.UserId == CurrentUserId)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryReadDto { Id = c.Id, Name = c.Name, Color = c.Color, Icon = c.Icon })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<ActionResult<CategoryReadDto>> Create(CategoryCreateDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Color = dto.Color,
                Icon = dto.Icon,
                UserId = CurrentUserId
            };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return Ok(new CategoryReadDto { Id = category.Id, Name = category.Name, Color = category.Color, Icon = category.Icon });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CategoryCreateDto dto)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (category == null) return NotFound();

            category.Name = dto.Name;
            category.Color = dto.Color;
            category.Icon = dto.Icon;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (category == null) return NotFound();

            var hasExpenses = await _db.Expenses.AnyAsync(e => e.CategoryId == id);
            if (hasExpenses)
                return BadRequest(new { message = "Cannot delete category with existing expenses." });

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
