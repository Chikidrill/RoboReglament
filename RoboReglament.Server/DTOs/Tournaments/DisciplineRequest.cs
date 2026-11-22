using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.DTOs.Tournaments;

public class DisciplineRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}