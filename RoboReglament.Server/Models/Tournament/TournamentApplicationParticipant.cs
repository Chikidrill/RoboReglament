using Microsoft.Identity.Client;
using RoboReglament.Server.Models;
namespace RoboReglament.Server.Models
{
    public class TournamentApplicationParticipant
    {
        public int Id { get; set; }

        public int TournamentApplicationId { get; set; }

        public TournamentApplication TournamentApplication { get; set; } = null!;

        public int? ParticipantId { get; set; }

        public Participant? Participant { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public DateOnly? BirthDate { get; set; }
    }
}
