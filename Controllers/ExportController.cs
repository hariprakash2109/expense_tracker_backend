using ExpenseTracker.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    public class ExportController : ApiControllerBase
    {
        private readonly AppDbContext _db;

        public ExportController(AppDbContext db)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _db = db;
        }

        [HttpGet("pdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] int year, [FromQuery] int month)
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var expenses = await _db.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == CurrentUserId && e.Date >= start && e.Date < end)
                .OrderBy(e => e.Date)
                .ToListAsync();

            var total = expenses.Sum(e => e.Amount);
            var monthName = start.ToString("MMMM yyyy");

            var categoryTotals = expenses
                .GroupBy(e => e.Category!.Name)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Expense Report").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"{monthName} — {user.Name}").FontSize(12).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Total Spent").FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"₹{total:N2}").FontSize(16).Bold().FontColor(Colors.Red.Darken1);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Budget").FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"₹{user.MonthlyBudget:N2}").FontSize(16).Bold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Remaining").FontColor(Colors.Grey.Darken1);
                                var remaining = user.MonthlyBudget - total;
                                c.Item().Text($"₹{remaining:N2}").FontSize(16).Bold()
                                    .FontColor(remaining < 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                            });
                        });

                        col.Item().PaddingTop(20).Text("Category Breakdown").FontSize(13).Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Category").Bold();
                                h.Cell().Text("Amount").Bold();
                                h.Cell().Text("% of Total").Bold();
                                h.Cell().ColumnSpan(3).PaddingBottom(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
                            });

                            foreach (var c in categoryTotals)
                            {
                                var pct = total == 0 ? 0 : (c.Total / total) * 100;
                                table.Cell().PaddingVertical(3).Text(c.Category);
                                table.Cell().PaddingVertical(3).Text($"₹{c.Total:N2}");
                                table.Cell().PaddingVertical(3).Text($"{pct:N1}%");
                            }
                        });

                        col.Item().PaddingTop(25).Text("Transaction Details").FontSize(13).Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Date").Bold();
                                h.Cell().Text("Title").Bold();
                                h.Cell().Text("Category").Bold();
                                h.Cell().Text("Amount").Bold();
                                h.Cell().ColumnSpan(4).PaddingBottom(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
                            });

                            foreach (var e in expenses)
                            {
                                table.Cell().PaddingVertical(3).Text(e.Date.ToString("dd MMM yyyy"));
                                table.Cell().PaddingVertical(3).Text(e.Title);
                                table.Cell().PaddingVertical(3).Text(e.Category!.Name);
                                table.Cell().PaddingVertical(3).Text($"₹{e.Amount:N2}");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated on ").FontSize(8);
                        x.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(8);
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Expense_Report_{monthName.Replace(" ", "_")}.pdf");
        }
    }
}
