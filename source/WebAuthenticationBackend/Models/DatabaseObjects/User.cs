using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using WebAuthenticationBackend.Data;

namespace WebAuthenticationBackend.Models.DatabaseObjects
{
    public class User
    {
        [Column("id_user")]
        public int Id { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; } = null!;

        [Column("last_name")]
        public string LastName { get; set; } = null!;

        [Column("email")]
        public string Email { get; set; } = null!;

        [Column("salt")]
        public string Salt { get; set; } = null!;

        [Column("hash")]
        public string Hash { get; set; } = null!;

        [Column("two_fa_key")]
        public string? TwoFaKey { get; set; }

        [Column("two_fa_uri")]
        public string? TwoFaUri { get; set; }


        public int GenerateUniqueId(AppDbContext context)
        {
            Random random = new();
            int id;
            do
            {
                id = random.Next(1_000_000, 9_999_999);
            }while(context.Users.Any(u => u.Id == id));
            return id;
        }
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? TwoFaKey { get; set; }
        public string? TwoFaUri { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
        public string? TwoFaKey { get; set; }
        public string? TwoFaUri { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
