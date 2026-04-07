using System.ComponentModel.DataAnnotations;

namespace DLL_ConnectionToDatabase;

public class Genre : IEntityWithId
{
    public int Id { get; set; }

    [MaxLength(30)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public List<Concept> Concepts { get; set; } = [];

}