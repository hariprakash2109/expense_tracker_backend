using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    public class ReportsController : ApiControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("monthly")]
        public async Task<ActionResult<MonthlyReportDto>> GetMonthlyReport([FromQuery] int year, [FromQuery] int month)
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var expenses = await _db.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == CurrentUserId && e.Date >= start && e.Date < end)
                .ToListAsync();

            var total = expenses.Sum(e => e.Amount);

            var breakdown = expenses
                .GroupBy(e => new { e.CategoryId, e.Category!.Name, e.Category.Color })
                .Select(g => new CategoryBreakdownDto
                {
                    CategoryName = g.Key.Name,
                    Color = g.Key.Color,
                    Total = g.Sum(x => x.Amount),
                    Percentage = total == 0 ? 0 : Math.Round((double)(g.Sum(x => x.Amount) / total) * 100, 1)
                })
                .OrderByDescending(b => b.Total)
                .ToList();

            var dailyTotals = expenses
                .GroupBy(e => e.Date.Date)
                .Select(g => new DailyTotalDto { Date = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderBy(d => d.Date)
                .ToList();

            var report = new MonthlyReportDto
            {
                Year = year,
                Month = month,
                TotalSpent = total,
                Budget = user.MonthlyBudget,
                Remaining = user.MonthlyBudget - total,
                BudgetExceeded = user.MonthlyBudget > 0 && total > user.MonthlyBudget,
                CategoryBreakdown = breakdown,
                DailyTotals = dailyTotals
            };

            return Ok(report);
        }

        [HttpGet("yearly-trend")]
        public async Task<ActionResult> GetYearlyTrend([FromQuery] int year)
        {
            var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddYears(1);

            var expenses = await _db.Expenses
                .Where(e => e.UserId == CurrentUserId && e.Date >= start && e.Date < end)
                .ToListAsync();

            var monthly = Enumerable.Range(1, 12).Select(m => new
            {
                Month = m,
                Total = expenses.Where(e => e.Date.Month == m).Sum(e => e.Amount)
            });

            return Ok(monthly);
        }

        [HttpGet("budget-status")]
        public async Task<ActionResult> GetBudgetStatus()
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            var now = DateTime.UtcNow;
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var total = await _db.Expenses
                .Where(e => e.UserId == CurrentUserId && e.Date >= start && e.Date < end)
                .SumAsync(e => e.Amount);

            var percentUsed = user.MonthlyBudget > 0 ? Math.Round((double)(total / user.MonthlyBudget) * 100, 1) : 0;

            return Ok(new
            {
                budget = user.MonthlyBudget,
                spent = total,
                remaining = user.MonthlyBudget - total,
                percentUsed,
                exceeded = user.MonthlyBudget > 0 && total > user.MonthlyBudget,
                nearLimit = percentUsed >= 80 && percentUsed < 100
            });
        }

        [HttpPut("budget")]
        public async Task<IActionResult> UpdateBudget(BudgetUpdateDto dto)
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            user.MonthlyBudget = dto.MonthlyBudget;
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
