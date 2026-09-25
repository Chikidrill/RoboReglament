namespace RoboReglament.Server.Models;

public class ProtocolCriterion
{
    public int Id { get; set; }

    public int ProtocolTemplateId { get; set; }

    public ProtocolTemplate ProtocolTemplate { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal MinValue { get; set; } = 0;

    public decimal? MaxValue { get; set; }

    public decimal Weight { get; set; } = 1;

    public bool IsPenalty { get; set; }

    public int Order { get; set; }
    public ICollection<JudgeScore> JudgeScores { get; set; }
        = new List<JudgeScore>();
}