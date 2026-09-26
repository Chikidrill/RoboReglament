using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Tournaments
{
    public class TournamentResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Location { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public DateTime? RegistrationDeadline { get; set; }

        public TournamentStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
