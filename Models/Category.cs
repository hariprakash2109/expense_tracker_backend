using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Color { get; set; } = "#6366f1";

        [MaxLength(50)]
        public string Icon { get; set; } = "tag";

        public int UserId { get; set; }
        public User? User { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
