namespace RoboReglament.Server.Models;

public class TournamentApplication
{
    public int Id { get; set; }

    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    public int TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public ApplicationStatus Status { get; set; }
        = ApplicationStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Accreditation? Accreditation { get; set; }
    public string? Comment { get; set; }
}