namespace RoboReglament.Server.Models
{
    public class Team
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Organization { get; set; }

        public string? City { get; set; }

        public ICollection<Participant> Participants { get; set; }
            = new List<Participant>();

        public ICollection<TournamentApplication> Applications { get; set; }
            = new List<TournamentApplication>();

        public ICollection<MatchTeam> MatchTeams { get; set; }
            = new List<MatchTeam>();
    }
}
