namespace DLL_ConnectionToDatabase
{
    public class ConceptImages : IEntityWithId
    {
        public int Id { get; set; }
        public required string Image { get; set; }

        public Concept Concept { get; set; }
        public required int ConceptId { get; set; }
    }
}
