namespace RoboReglament.Server.DTOs.Tournaments;

public class DisciplineResponse
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}