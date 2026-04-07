namespace DLL_ConnectionToDatabase
{
    public class Review : IEntityWithId
    {
        public int Id { get; set; }

        public bool IsLiked { get; set; }

        public UserInfo? User { get; set; }
        public int? UserId { get; set; }

        public Concept Concept { get; set; }
        public required int ConceptId { get; set; }
    }
}
