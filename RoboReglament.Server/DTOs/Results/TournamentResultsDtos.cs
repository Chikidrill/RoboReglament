
using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Results;

public sealed class TournamentResultsResponse
{
    public int TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public TournamentStatus Status { get; set; }

    public List<DisciplineResultsResponse> Disciplines { get; set; }
        = new();
}

public sealed class DisciplineResultsResponse
{
    public int DisciplineId { get; set; }
    public string Name { get; set; } = string.Empty;

    public CompetitionFormat Format { get; set; }
    public ScoringMode ScoringMode { get; set; }

    public JudgeAggregationMethod AggregationMethod { get; set; }

    public List<MatchResultResponse> Matches { get; set; }
        = new();

    public RankingMethod RankingMethod { get; set; }

    public List<TeamRankingResponse> Standings { get; set; }
        = new();
    public int WinPoints { get; set; }
    public int DrawPoints { get; set; }
    public int LossPoints { get; set; }
}

public sealed class MatchResultResponse
{
    public int MatchId { get; set; }
    public int Number { get; set; }
    public string? Name { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public bool IsDraw { get; set; }

    public List<SideResultResponse> Sides { get; set; }
        = new();
}

public sealed class SideResultResponse
{
    public int SideId { get; set; }
    public int Number { get; set; }
    public string? Name { get; set; }

    public decimal TotalScore { get; set; }
    public int JudgeCount { get; set; }

    public bool IsWinner { get; set; }

    public List<ResultTeamResponse> Teams { get; set; }
        = new();
}

public sealed class ResultTeamResponse
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
}
