using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.DTOs.Teams
{
    public class UpdateTeamRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Organization { get; set; }

        [MaxLength(200)]
        public string? City { get; set; }
    }
}
