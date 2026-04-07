using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DLL_ConnectionToDatabase
{
    public class Concept : IEntityWithId
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(50)]
        public required string Title { get; set; }

        public string? Description { get; set; }

        public DateTime CreationDate { get; set; }
        public DateTime ChangeDate { get; set; }

        public UserInfo User { get; set; }
        public required int UserId { get; set; }

        public List<ConceptImages> ConceptImages { get; set; } = [];
        public List<Genre> Genres { get; set; } = [];
        public List<Comment> Comments { get; set; } = [];
        public List<Review> Reviews { get; set; } = [];
    }
}
