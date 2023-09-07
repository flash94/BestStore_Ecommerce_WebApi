using Microsoft.EntityFrameworkCore;

namespace BestStoreApi.Models
{
    [Index ("Email", IsUnique = true)]
    public class PasswordReset
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
