
using System.ComponentModel.DataAnnotations;
using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Judging;

public sealed class SaveJudgeProtocolRequest
{
    [Required]
    public List<JudgeScoreInput> Scores { get; set; } = new();

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public sealed class JudgeScoreInput
{
    [Range(1, int.MaxValue)]
    public int CriterionId { get; set; }

    [Required]
    public decimal? Value { get; set; }
}

public sealed class JudgeCriterionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal Weight { get; set; }
    public bool IsPenalty { get; set; }
    public int Order { get; set; }
}

public sealed class JudgeScoreResponse
{
    public int CriterionId { get; set; }
    public decimal Value { get; set; }
}

public sealed class JudgeProtocolResponse
{
    public int Id { get; set; }
    public int SideId { get; set; }

    public JudgeProtocolStatus Status { get; set; }

    public string? Comment { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public decimal? Total { get; set; }

    public List<JudgeScoreResponse> Scores { get; set; } = new();
}

public sealed class JudgeSideScorecardResponse
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string? Name { get; set; }

    public List<string> Teams { get; set; } = new();

    public JudgeProtocolResponse? Protocol { get; set; }
}

public sealed class JudgeMatchScorecardResponse
{
    public int MatchId { get; set; }
    public string? MatchName { get; set; }

    public MatchStatus Status { get; set; }

    public CompetitionFormat Format { get; set; }
    public ScoringMode ScoringMode { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public List<JudgeCriterionResponse> Criteria { get; set; }
        = new();

    public List<JudgeSideScorecardResponse> Sides { get; set; }
        = new();
}
