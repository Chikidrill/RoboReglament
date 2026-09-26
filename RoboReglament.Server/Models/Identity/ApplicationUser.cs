using Microsoft.AspNetCore.Identity;
namespace RoboReglament.Server.Models.Identity
{
    public class ApplicationUser: IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }
        public ICollection<TournamentOrganizer> OrganizedTournaments { get; set; }
            = new List<TournamentOrganizer>();
    }
}
