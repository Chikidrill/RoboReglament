namespace RoboReglament.Server.Models;
public class Participant
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public int TeamId { get; set; }

    public Team Team { get; set; } = null!;
}