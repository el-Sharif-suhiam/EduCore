
namespace Common.Dtos
{
    public class DtoUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PasswordHash { get; set; }
        public string RefreshTokenHash { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenRevokedAt { get; set; }
        public bool IsActive { get; set; }

    }
}
