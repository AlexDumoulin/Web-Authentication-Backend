using System.ComponentModel.DataAnnotations.Schema;

namespace WebAuthenticationBackend.Models.DatabaseObjects
{
    public class RefreshToken
    {
        [Column("id_refreshToken")]
        public int Id { get; set; }

        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [Column("id_user")]
        public int UserId { get; set; }

        public User User { get; set; } = null!;
    }
}
