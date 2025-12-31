using System.ComponentModel.DataAnnotations.Schema;
using WebAuthenticationBackend.Data;

namespace WebAuthenticationBackend.Models.DatabaseObjects
{
    public class JwtUser
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("salt")]
        public string Salt { get; set; } = null!;

        [Column("hash")]
        public string Hash { get; set; } = null!;

        public int GenerateUniqueId(AppDbContext context)
        {
            Random random = new();
            int id;
            do
            {
                id = random.Next(1_000_000, 9_999_999);
            } while (context.JwtUsers.Any(u => u.Id == id));
            return id;
        }
    }

    public class CreateJwtUserRequest
    {
        public string Name { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class GetJwt
    {
        public string Name {  set; get; } = null!;
        public string Password { get; set; } = null!;
    }
}
