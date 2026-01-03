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

        [HttpGet("jwt_user/{id}")]
        public async Task<IActionResult> GetJwtUserByIdAsync(int id)
        {
            var user = await _context.JwtUsers.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost("jwt_user")]
        public async Task<IActionResult> CreateUserAsync(
            [FromBody] CreateJwtUserRequest request)
        {
            var normalizedName = request.Name.Trim().ToLower();


            var existingJwtUser = await _context.JwtUsers
                .FirstOrDefaultAsync(u => u.Name == normalizedName);

            if (existingJwtUser != null)
                return BadRequest("Name already in use.");

            var salt = HashingService.GenerateSalt();
            var hash = HashingService.ComputeHash(request.Password, salt);

            var newJwtUser = new JwtUser
            {
                Name = normalizedName,
                Salt = salt,
                Hash = hash,
            };

            newJwtUser.Id = newJwtUser.GenerateUniqueId(_context);

            _context.JwtUsers.Add(newJwtUser);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Name already in use.");
            }
            return CreatedAtAction(
                nameof(GetJwtUserByIdAsync),
                new { id = newJwtUser.Id },
                new { newJwtUser.Id, newJwtUser.Name }
            );
        }

        [HttpPost("get_jwt")]
        public async Task<IActionResult> GetJwtAsync(
            [FromBody] GetJwt request)
        {
            var normalizedName = request.Name.Trim().ToLower();
            var jwtUser = await _context.JwtUsers.
                FirstOrDefaultAsync(jwt => jwt.Name == normalizedName);

            if (jwtUser != null) { 
                var hash = HashingService.ComputeHash(request.Password,
                    jwtUser.Salt);
                if (normalizedName == jwtUser.Name && hash == jwtUser.Hash)
                {
                    var token = GenerateJwt(normalizedName);
                    return Ok(new { token });
                }
            }
            return Unauthorized();
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
        public async Task<IActionResult> LoginAsync(
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

        private string GenerateJwt(string username)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"]!,
                audience: _config["Jwt:Audiance"]!,
                claims: new[] { new Claim(ClaimTypes.Name, username) },
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(_config["Jwt:ExpireMinutes"]!)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
