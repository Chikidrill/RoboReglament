namespace RoboReglament.Server.DTOs.Teams
{
    public class TeamResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Organization { get; set; }
        public string? City { get; set; }
        public int ParticipantsCount { get; set; }

    }
}
