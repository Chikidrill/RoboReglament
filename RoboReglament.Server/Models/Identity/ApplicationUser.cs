
using Microsoft.AspNetCore.Identity;
using RoboReglament.Server.Models;

namespace RoboReglament.Server.Models.Identity;

public class ApplicationUser : IdentityUser
{   
    public long PublicId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public ICollection<TournamentOrganizer> OrganizedTournaments
    { get; set; } = new List<TournamentOrganizer>();

    public ICollection<TeamCoach> CoachedTeams
    { get; set; } = new List<TeamCoach>();

    public ICollection<Participant> TeamMemberships
    { get; set; } = new List<Participant>();
}
