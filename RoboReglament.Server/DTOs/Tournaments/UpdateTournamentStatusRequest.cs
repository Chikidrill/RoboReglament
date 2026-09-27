using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Tournaments
{
    public class UpdateTournamentStatusRequest
    {
        public TournamentStatus Status { get; set; }
    }
}
