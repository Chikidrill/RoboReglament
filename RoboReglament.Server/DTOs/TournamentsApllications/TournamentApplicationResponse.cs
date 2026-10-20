namespace RoboReglament.Server.DTOs.TournamentApplications;

public class TournamentApplicationResponse
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? Comment { get; set; }

    public bool IsAccredited { get; set; }
    public DateTime? AccreditedAt { get; set; }
    public string? AccreditationComment { get; set; }

    public List<ApplicationParticipantResponse> Participants { get; set; }
        = new();
}