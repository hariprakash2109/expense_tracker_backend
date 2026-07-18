using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    public class ExpensesController : ApiControllerBase
    {
        private readonly AppDbContext _db;

        public ExpensesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<ExpenseReadDto>>> GetAll(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? categoryId,
            [FromQuery] string? search)
        {
            var query = _db.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == CurrentUserId);

            if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
            if (to.HasValue) query = query.Where(e => e.Date <= to.Value);
            if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Title.ToLower().Contains(search.ToLower()));

            var expenses = await query
                .OrderByDescending(e => e.Date)
                .Select(e => new ExpenseReadDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Amount = e.Amount,
                    Date = e.Date,
                    Notes = e.Notes,
                    PaymentMethod = e.PaymentMethod,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category!.Name,
                    CategoryColor = e.Category.Color
                })
                .ToListAsync();

            return Ok(expenses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseReadDto>> GetById(int id)
        {
            var e = await _db.Expenses.Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);

            if (e == null) return NotFound();

            return Ok(new ExpenseReadDto
            {
                Id = e.Id,
                Title = e.Title,
                Amount = e.Amount,
                Date = e.Date,
                Notes = e.Notes,
                PaymentMethod = e.PaymentMethod,
                CategoryId = e.CategoryId,
                CategoryName = e.Category!.Name,
                CategoryColor = e.Category.Color
            });
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseReadDto>> Create(ExpenseCreateDto dto)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == CurrentUserId);
            if (category == null) return BadRequest(new { message = "Invalid category." });

            var expense = new Expense
            {
                Title = dto.Title,
                Amount = dto.Amount,
                Date = dto.Date,
                Notes = dto.Notes,
                PaymentMethod = dto.PaymentMethod,
                CategoryId = dto.CategoryId,
                UserId = CurrentUserId
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            return Ok(new ExpenseReadDto
            {
                Id = expense.Id,
                Title = expense.Title,
                Amount = expense.Amount,
                Date = expense.Date,
                Notes = expense.Notes,
                PaymentMethod = expense.PaymentMethod,
                CategoryId = expense.CategoryId,
                CategoryName = category.Name,
                CategoryColor = category.Color
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ExpenseUpdateDto dto)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense == null) return NotFound();

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == CurrentUserId);
            if (category == null) return BadRequest(new { message = "Invalid category." });

            expense.Title = dto.Title;
            expense.Amount = dto.Amount;
            expense.Date = dto.Date;
            expense.Notes = dto.Notes;
            expense.PaymentMethod = dto.PaymentMethod;
            expense.CategoryId = dto.CategoryId;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense == null) return NotFound();

            _db.Expenses.Remove(expense);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
