using RoboReglament.Server.Models.Identity;
namespace RoboReglament.Server.Models;

public class JudgeProtocol
{
    public int Id { get; set; }

    public int MatchGroupId { get; set; }

    public MatchGroup MatchGroup { get; set; } = null!;

    public string? JudgeId { get; set; }
    public ApplicationUser? Judge { get; set; }
    public JudgeProtocolStatus Status { get; set; }
        = JudgeProtocolStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAt { get; set; }

    public string? Comment { get; set; }

    public ICollection<JudgeScore> Scores { get; set; }
        = new List<JudgeScore>();
}