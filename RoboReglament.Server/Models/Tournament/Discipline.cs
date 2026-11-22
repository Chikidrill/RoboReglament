
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
}
