using Microsoft.VisualBasic;
using System.Text.RegularExpressions;

namespace RoboReglament.Server.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateOnly StartDate { get;set;  }
        public DateOnly EndDate { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public TournamentStatus Status { get; set; } = TournamentStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TournamentApplication> Applications { get; set; }
        = new List<TournamentApplication>();

        public ICollection<Match> Matches { get; set; }
            = new List<Match>();

        public ICollection<ProtocolTemplate> ProtocolTemplates { get; set; }
            = new List<ProtocolTemplate>();
    }
}
