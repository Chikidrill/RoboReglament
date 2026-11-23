
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Models;

public class MatchJudge
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public string JudgeId { get; set; } = string.Empty;
    public ApplicationUser Judge { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}