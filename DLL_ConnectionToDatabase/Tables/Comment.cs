using System.ComponentModel.DataAnnotations;

namespace DLL_ConnectionToDatabase
{
    public class Comment : IEntityWithId
    {
        public int Id { get; set; }

        [MaxLength(300)]
        public string? Content { get; set; }

        public int NumberOfLikes { get; set; }
        public int NumberOfDislikes { get; set; }

        public DateTime CreationDate { get; set; }

        public UserInfo User { get; set; }
        public required int UserId { get; set; }

        public Concept Concept { get; set; }
        public required int ConceptId { get; set; }
    }
}
