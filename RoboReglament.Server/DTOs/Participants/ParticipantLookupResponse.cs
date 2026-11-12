
namespace RoboReglament.Server.DTOs.Participants;

public class ParticipantLookupResponse
{
    public long PublicId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public bool AlreadyInTeam { get; set; }
}
