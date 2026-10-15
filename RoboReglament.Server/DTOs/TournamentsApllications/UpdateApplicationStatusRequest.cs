
using System.ComponentModel.DataAnnotations;
using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.TournamentApplications;

public class UpdateApplicationStatusRequest
{
    public ApplicationStatus Status { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}
