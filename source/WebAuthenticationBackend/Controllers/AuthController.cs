using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebAuthenticationBackend.Data;
using WebAuthenticationBackend.Models;
using WebAuthenticationBackend.Models.DatabaseObjects;

namespace WebAuthenticationBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
    
        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUser()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Salt,
                    u.Hash
                }).ToListAsync();

            return Ok(users);
        }

        [HttpPost("user")]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();

            // Regex for email validation
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(normalizedEmail, emailPattern))
                return BadRequest("Invalid email format.");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (existingUser != null)
                return BadRequest("Email already in use.");

            var salt = HashingService.GenerateSalt();
            var hash = HashingService.ComputeHash(request.Password, salt);

            var newUser = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = normalizedEmail,
                Salt = salt,
                Hash = hash,
                TwoFaKey = request.TwoFaKey,
                TwoFaUri = request.TwoFaUri,
            };

            newUser.Id = newUser.GenerateUniqueId(_context);

            _context.Users.Add(newUser);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Email already in use.");
            }
            return CreatedAtAction(
                nameof(GetUserById),
                new { id = newUser.Id },
                new { newUser.Id, newUser.Email }
            );
        }

        [HttpPatch("user/{id}")]
        public async Task<IActionResult> UpdateUserById(int id,
            [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (request.FirstName != null)
                user.FirstName = request.FirstName;
            if (request.LastName != null)
                user.LastName = request.LastName;
            if (request.Password != null)
            {
                var salt = HashingService.GenerateSalt();
                var hash = HashingService.ComputeHash(request.Password, salt);
                user.Salt = salt;
                user.Hash = hash;
            }
            if (request.TwoFaKey != null)
                user.TwoFaKey = request.TwoFaKey;
            if (request.TwoFaUri != null)
                user.TwoFaUri = request.TwoFaUri;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUserById(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Id == id);
            if (user == null)
                return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Unauthorized("Invalid email or password.");
            var hash = HashingService.ComputeHash(request.Password, user.Salt);
            if (hash != user.Hash)
                return Unauthorized("Invalid email or password.");

            return Ok("Login successful.");
        }
    }
}
