namespace RoboReglament.Server.Models;

public class ProtocolTemplate
{
    public int Id { get; set; }

    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProtocolCriterion> Criteria { get; set; }
        = new List<ProtocolCriterion>();

    public ICollection<Match> Matches { get; set; }
        = new List<Match>();
}