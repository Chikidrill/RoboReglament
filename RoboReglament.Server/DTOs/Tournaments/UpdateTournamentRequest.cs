using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.DTOs.Tournaments
{
    public class UpdateTournamentRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? Location { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public DateTime? RegistrationDeadline { get; set; }
    }
}
