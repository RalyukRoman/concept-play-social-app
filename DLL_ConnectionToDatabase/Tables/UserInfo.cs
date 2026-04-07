using System.ComponentModel.DataAnnotations;

namespace DLL_ConnectionToDatabase
{
    public class UserInfo : IEntityWithId
    {
        public int Id { get; set; }

        [MaxLength(30)]
        public required string Username { get; set; }

        [MaxLength(30)]
        public required string Email { get; set; }

        [MaxLength(30)]
        public required string Password { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreationDate { get; set; }

        public string? AvatarImage { get; set; }

        public bool IsAdmin { get; set; }

        public DateOnly? BannedUntil { get; set; }

        public UserInfo? BannedBy { get; set; }
        public int? BannedById { get; set; }

        public List<Concept> Concepts { get; set; } = [];

        public List<Comment> Comments { get; set; } = [];

        public List<Review> Reviews { get; set; } = [];
    }
}
