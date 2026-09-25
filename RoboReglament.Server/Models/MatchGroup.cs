namespace RoboReglament.Server.Models;

public class MatchGroup
{
    public int Id { get; set; }

    public int MatchId { get; set; }

    public Match Match { get; set; } = null!;

    public int Number { get; set; }

    public string? Name { get; set; }

    public ICollection<MatchTeam> MatchTeams { get; set; }
        = new List<MatchTeam>();
    public ICollection<JudgeProtocol> JudgeProtocols { get; set; }
        = new List<JudgeProtocol>();
}