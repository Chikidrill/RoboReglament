using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.DTOs.TournamentApplications;

public class AccreditApplicationRequest
{
    [MaxLength(1000)]
    public string? Comment { get; set; }
}