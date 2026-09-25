namespace RoboReglament.Server.Models;

public class JudgeScore
{
    public int Id { get; set; }

    public int JudgeProtocolId { get; set; }

    public JudgeProtocol JudgeProtocol { get; set; } = null!;

    public int ProtocolCriterionId { get; set; }

    public ProtocolCriterion ProtocolCriterion { get; set; } = null!;

    public decimal Value { get; set; }

    public string? Comment { get; set; }
}