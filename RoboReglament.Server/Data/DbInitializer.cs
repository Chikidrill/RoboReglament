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
                UserRoles.Administrator,
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
        public static async Task InitializeAdminAsync(
                IServiceProvider serviceProvider,
                IConfiguration configuration)
        {
            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var email = configuration["Admin:Email"];
            var password = configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var existingAdmin =
                await userManager.FindByEmailAsync(email);

            if (existingAdmin is not null)
            {
                return;
            }

            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true
            };

            var result =
                await userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        result.Errors.Select(x => x.Description)));
            }

            await userManager.AddToRoleAsync(
                admin,
                UserRoles.Administrator);
        }
    }
}
