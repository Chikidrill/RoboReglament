
using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Tournaments;

public class DisciplineResponse
{
    public int Id { get; set; }
    public int TournamentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CompetitionFormat Format { get; set; }
    public ScoringMode ScoringMode { get; set; }

    public int SidesPerEvent { get; set; }

    public int MinTeamsPerSide { get; set; }
    public int MaxTeamsPerSide { get; set; }

    public JudgeAggregationMethod JudgeAggregationMethod { get; set; }
    public RankingMethod RankingMethod { get; set; }


    public int WinPoints { get; set; }
    public int DrawPoints { get; set; }
    public int LossPoints { get; set; }

}
