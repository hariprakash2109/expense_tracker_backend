using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email already registered." });

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Seed default categories for new user
            var defaults = new[]
            {
                new Category { Name = "Food & Dining", Color = "#ef4444", Icon = "utensils", UserId = user.Id },
                new Category { Name = "Transportation", Color = "#3b82f6", Icon = "car", UserId = user.Id },
                new Category { Name = "Shopping", Color = "#f59e0b", Icon = "shopping-bag", UserId = user.Id },
                new Category { Name = "Entertainment", Color = "#8b5cf6", Icon = "film", UserId = user.Id },
                new Category { Name = "Bills & Utilities", Color = "#10b981", Icon = "file-text", UserId = user.Id },
                new Category { Name = "Health", Color = "#ec4899", Icon = "heart", UserId = user.Id },
                new Category { Name = "Other", Color = "#6b7280", Icon = "tag", UserId = user.Id },
            };
            _db.Categories.AddRange(defaults);
            await _db.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);
            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                MonthlyBudget = user.MonthlyBudget
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            var token = _tokenService.GenerateToken(user);
            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                MonthlyBudget = user.MonthlyBudget
            });
        }
    }
}
