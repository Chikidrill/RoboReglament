
using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.DTOs.Protocols;

public sealed class SaveProtocolTemplateRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MinLength(1)]
    public List<SaveProtocolCriterionRequest> Criteria { get; set; }
        = new();
}

public sealed class SaveProtocolCriterionRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal MinValue { get; set; } = 0;

    public decimal MaxValue { get; set; }

    public decimal Weight { get; set; } = 1;

    public bool IsPenalty { get; set; }
}

public sealed class ProtocolTemplateResponse
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public int DisciplineId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<ProtocolCriterionResponse> Criteria { get; set; }
        = new();
}

public sealed class ProtocolCriterionResponse
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

public sealed class PreviewProtocolRequest
{
    public List<PreviewScoreRequest> Scores { get; set; } = new();
}

public sealed class PreviewScoreRequest
{
    public int CriterionId { get; set; }
    public decimal Value { get; set; }
}

public sealed class PreviewProtocolResponse
{
    public int TemplateId { get; set; }
    public decimal Total { get; set; }
    public decimal MaximumPossible { get; set; }

    public List<PreviewCriterionResponse> Criteria { get; set; }
        = new();
}

public sealed class PreviewCriterionResponse
{
    public int CriterionId { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal Value { get; set; }
    public decimal Weight { get; set; }
    public bool IsPenalty { get; set; }

    public decimal Contribution { get; set; }
}
