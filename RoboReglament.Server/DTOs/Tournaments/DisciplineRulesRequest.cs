
using System.ComponentModel.DataAnnotations;
using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Tournaments;

public class DisciplineRulesRequest
{
    [Required]
    public CompetitionFormat? Format { get; set; }

    [Required]
    public ScoringMode? ScoringMode { get; set; }

    [Range(1, 4)]
    public int SidesPerEvent { get; set; }

    [Range(1, 4)]
    public int MinTeamsPerSide { get; set; }

    [Range(1, 4)]
    public int MaxTeamsPerSide { get; set; }
}
