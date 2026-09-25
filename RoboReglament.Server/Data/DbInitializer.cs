using Microsoft.AspNetCore.Identity;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Data
{
    public class DbInitializer
    {
        public static async Task InitializeRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles =
            [
                UserRoles.Organizer,
            UserRoles.Judge,
            UserRoles.Coach,
            UserRoles.Participant
            ];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
