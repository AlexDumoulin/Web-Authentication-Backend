using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

        //[Authorize]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        //[Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUserAsync()
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

        //[Authorize]
        [HttpPost("user")]
        public async Task<IActionResult> CreateUserAsync(
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
            return StatusCode(201, new { newUser.Id, newUser.Email });
        }

        //[Authorize]
        [HttpPatch("user/{id}")]
        public async Task<IActionResult> UpdateUserByIdAsync(int id,
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

        //[Authorize]
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUserByIdAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Id == id);
            if (user == null)
                return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        //[Authorize]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || HashingService.ComputeHash(request.Password, user.Salt) != user.Hash)
                return Unauthorized("Invalid credentials.");

            // 1. Generate short-lived Access Token (JWT)
            var accessToken = GenerateJwt(user.Email);

            // 2. Generate long-lived Refresh Token
            var refreshToken = Guid.NewGuid().ToString();

            // 3. Save Refresh Token to DB
            var rtEntry = new RefreshToken
            {
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
            };
            _context.RefreshTokens.Add(rtEntry);
            await _context.SaveChangesAsync();

            // 4. Send Refresh Token in Cookie, Access Token in JSON
            SetRefreshToken(refreshToken);

            return Ok(new { accessToken });
        }

        [HttpGet("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken)) return Unauthorized();

            var storedToken = await _context.RefreshTokens
                .Include(t => t.User) // This works now!
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (storedToken == null || storedToken.ExpiryDate < DateTime.UtcNow)
                return Unauthorized("Session expired.");

            // Generate a new JWT using the Email from the linked User object
            var newAccessToken = GenerateJwt(storedToken.User.Email);

            return Ok(new { accessToken = newAccessToken });
        }

        private string GenerateJwt(string email)
        {
            // Use the exact key from your appsettings.json
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            // Create claims - using Email as the Name identifier is common
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, email),
        new Claim(ClaimTypes.Email, email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"]!,
                // Ensure this matches the "Audiance" spelling in your Program.cs
                audience: _config["Jwt:Audiance"]!,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_config["Jwt:ExpireMinutes"] ?? "60")),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetRefreshToken(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,        // Prevents JS access (XSS protection)
                Secure = true,          // Only sent over HTTPS
                SameSite = SameSiteMode.None, // Required for cross-origin (React on 5173)
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
}
