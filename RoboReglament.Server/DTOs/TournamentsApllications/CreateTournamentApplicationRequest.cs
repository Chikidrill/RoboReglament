using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.DTOs.TournamentsApllications
{
    public class CreateTournamentApplicationRequest
    {
        [Range(1, int.MaxValue)]
        public int TeamId { get; set; }
    }
}
