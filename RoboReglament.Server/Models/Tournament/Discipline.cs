
using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.Models;

public class Discipline
{
    public int Id { get; set; }

    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    // Заезд или матч.
    public CompetitionFormat Format { get; set; }
        = CompetitionFormat.Run;

    // По отдельным командам или по союзам.
    public ScoringMode ScoringMode { get; set; }
        = ScoringMode.PerTeam;

    // Количество соревнующихся сторон.
    public int SidesPerEvent { get; set; } = 1;

    // Минимальное количество команд в одной стороне.
    public int MinTeamsPerSide { get; set; } = 1;

    // Максимальное количество команд в одной стороне.
    public int MaxTeamsPerSide { get; set; } = 1;
}
