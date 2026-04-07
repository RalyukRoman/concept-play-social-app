namespace ConceptPlay_Server
{
    public class DbMessage
    {
        public required string MessageType { get; set; }
        public required string ActionType { get; set; }
        public required string Table { get; set; }
        public required int Id { get; set; }
    }

    public class ImageMessage
    {
        public required string MessageType { get; set; }
        public required string ImageName { get; set; }
        public string? Data { get; set; }
    }
}
