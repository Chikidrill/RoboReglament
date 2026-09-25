namespace RoboReglament.Server.Models;

public class MatchTeam
{
    public int Id { get; set; }

    public int MatchGroupId { get; set; }

    public MatchGroup MatchGroup { get; set; } = null!;

    public int TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public int? Position { get; set; }
}