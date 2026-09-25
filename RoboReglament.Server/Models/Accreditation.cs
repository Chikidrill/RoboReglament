namespace RoboReglament.Server.Models;

public class Accreditation
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public TournamentApplication Application { get; set; } = null!;

    public bool IsAccredited { get; set; }

    public DateTime? AccreditedAt { get; set; }

    public string? Comment { get; set; }
}