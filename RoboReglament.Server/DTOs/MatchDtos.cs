
using System.ComponentModel.DataAnnotations;
using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Matches;

public sealed class SaveMatchRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    public DateTime? ScheduledAt { get; set; }

    // null — использовать активный шаблон дисциплины.
    public int? ProtocolTemplateId { get; set; }

    [Required]
    public List<SaveMatchSideRequest> Sides { get; set; } = new();
}

public sealed class SaveMatchSideRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [Required]
    public List<int> TeamIds { get; set; } = new();
}


public sealed class DrawMatchesRequest
{
    [Required]
    [Range(1, 4)]
    public int? TeamsPerSide { get; set; }

    public int? ProtocolTemplateId { get; set; }

    [MaxLength(100)]
    public string? NamePrefix { get; set; }

    public MatchDrawMode Mode { get; set; }
        = MatchDrawMode.FixedAppearances;

    [Range(1, 30)]
    public int AppearancesPerTeam { get; set; } = 1;
}

public sealed class UpdateMatchStatusRequest
{
    public MatchStatus Status { get; set; }
}

public sealed class AssignJudgeRequest
{
    [Range(1, long.MaxValue)]
    public long PublicId { get; set; }
}

public sealed class EligibleMatchTeamResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class MatchResponse
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public int DisciplineId { get; set; }

    public CompetitionFormat Format { get; set; }
    public ScoringMode ScoringMode { get; set; }

    public int Number { get; set; }
    public string? Name { get; set; }
    public DateTime? ScheduledAt { get; set; }

    public MatchStatus Status { get; set; }

    public int? ProtocolTemplateId { get; set; }
    public string? ProtocolTemplateName { get; set; }

    public List<MatchSideResponse> Sides { get; set; } = new();
    public List<MatchJudgeResponse> Judges { get; set; } = new();
}

public sealed class MatchSideResponse
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string? Name { get; set; }

    public List<MatchTeamResponse> Teams { get; set; } = new();
}

public sealed class MatchTeamResponse
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int? Position { get; set; }
}

public sealed class MatchJudgeResponse
{
    public string Id { get; set; } = string.Empty;
    public long PublicId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public DateTime AssignedAt { get; set; }
}

public enum MatchDrawMode
{
    FixedAppearances,
    AllUnique
}