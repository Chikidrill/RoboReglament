using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Models
{
    public class TournamentOrganizer
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
