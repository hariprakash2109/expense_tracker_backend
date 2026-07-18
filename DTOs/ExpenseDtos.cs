using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.DTOs
{
    public class ExpenseCreateDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(30)]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        public int CategoryId { get; set; }
    }

    public class ExpenseUpdateDto : ExpenseCreateDto { }

    public class ExpenseReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
    }

    public class CategoryCreateDto
    {
        [Required, MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Color { get; set; } = "#6366f1";

        [MaxLength(50)]
        public string Icon { get; set; } = "tag";
    }

    public class CategoryReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class MonthlyReportDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal Budget { get; set; }
        public decimal Remaining { get; set; }
        public bool BudgetExceeded { get; set; }
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
        public List<DailyTotalDto> DailyTotals { get; set; } = new();
    }

    public class CategoryBreakdownDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public double Percentage { get; set; }
    }

    public class DailyTotalDto
    {
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
    }

    public class BudgetUpdateDto
    {
        [Range(0, double.MaxValue)]
        public decimal MonthlyBudget { get; set; }
    }
}
