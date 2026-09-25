namespace RoboReglament.Server.Models
{
    public class Match
    {
        public int Id { get; set; }

        public int TournamentId { get; set; }

        public Tournament Tournament { get; set; } = null!;

        public int Number { get; set; }

        public string? Name { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

        public int? ProtocolTemplateId { get; set; }

        public ProtocolTemplate? ProtocolTemplate { get; set; }

        public ICollection<MatchGroup> MatchGroups { get; set; }
            = new List<MatchGroup>();
    }
}