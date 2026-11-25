
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


    [Required]
    public JudgeAggregationMethod? JudgeAggregationMethod { get; set; }
    public RankingMethod? RankingMethod { get; set; }


    [Range(0, 100)]
    public int? WinPoints { get; set; }

    [Range(0, 100)]
    public int? DrawPoints { get; set; }

    [Range(0, 100)]
    public int? LossPoints { get; set; }

}
