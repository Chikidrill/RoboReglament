
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Data;

public static class DevelopmentTestSeeder
{
    private sealed record TestAccount(
        string Email,
        string FirstName,
        string LastName,
        string Role
    );

    public static async Task SeedAsync(
        IServiceProvider services,
        string password)
    {
        var users = services.GetRequiredService<
            UserManager<ApplicationUser>>();

        var db = services.GetRequiredService<
            ApplicationDbContext>();

        // Организатор, тренеры и судьи.
        var accounts = new[]
        {
            new TestAccount(
                "demo.organizer@example.test",
                "Олег", "Организаторов",
                UserRoles.Organizer),

            new TestAccount(
                "demo.coach1@example.test",
                "Александр", "Иванов",
                UserRoles.Coach),

            new TestAccount(
                "demo.coach2@example.test",
                "Мария", "Петрова",
                UserRoles.Coach),

            new TestAccount(
                "demo.coach3@example.test",
                "Дмитрий", "Соколов",
                UserRoles.Coach),

            new TestAccount(
                "demo.judge1@example.test",
                "Андрей", "Судейкин",
                UserRoles.Judge),

            new TestAccount(
                "demo.judge2@example.test",
                "Елена", "Судейкина",
                UserRoles.Judge),
        };

        var createdUsers = new Dictionary<
            string, ApplicationUser>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var account in accounts)
        {
            createdUsers[account.Email] =
                await EnsureUserAsync(
                    users, account, password);
        }

        var coachEmails = new[]
        {
            "demo.coach1@example.test",
            "demo.coach2@example.test",
            "demo.coach3@example.test"
        };

        var teamNames = new[]
        {
            "[ТЕСТ] Альфа",
            "[ТЕСТ] Импульс",
            "[ТЕСТ] ТехноБоты",
            "[ТЕСТ] КиберКоманда",
            "[ТЕСТ] РобоСтарт",
            "[ТЕСТ] Механика"
        };

        for (var teamIndex = 0;
             teamIndex < teamNames.Length;
             teamIndex++)
        {
            // По две команды на одного тренера.
            var coachEmail = coachEmails[teamIndex / 2];
            var coach = createdUsers[coachEmail];
            var teamName = teamNames[teamIndex];

            var team = await db.Teams
                .Include(x => x.Coaches)
                .SingleOrDefaultAsync(x =>
                    x.Name == teamName);

            if (team is null)
            {
                team = new Team
                {
                    Name = teamName,
                    Organization = "Тестовая лаборатория",
                    City = "Томск"
                };

                team.Coaches.Add(new TeamCoach
                {
                    UserId = coach.Id
                });

                db.Teams.Add(team);
                await db.SaveChangesAsync();
            }
            else if (!team.Coaches.Any(
                x => x.UserId == coach.Id))
            {
                throw new InvalidOperationException(
                    $"Команда {teamName} уже существует, " +
                    "но принадлежит другому тренеру.");
            }

            // Два зарегистрированных участника
            // в каждой команде.
            for (var member = 1; member <= 2; member++)
            {
                var number = teamIndex * 2 + member;
                var email =
                    $"demo.member{number:00}@example.test";

                var participantUser =
                    await EnsureUserAsync(
                        users,
                        new TestAccount(
                            email,
                            $"Участник{number:00}",
                            "Тестовый",
                            UserRoles.Participant),
                        password);

                var exists = await db.Participants
                    .AnyAsync(x =>
                        x.TeamId == team.Id &&
                        x.UserId == participantUser.Id);

                if (exists)
                    continue;

                db.Participants.Add(new Participant
                {
                    TeamId = team.Id,
                    UserId = participantUser.Id,
                    FirstName = participantUser.FirstName,
                    LastName = participantUser.LastName,
                    MiddleName = participantUser.MiddleName
                });

                await db.SaveChangesAsync();
            }
        }

        Console.WriteLine(
            "=== Тестовые данные RoboReglament готовы ===");

        foreach (var account in accounts)
        {
            var user = createdUsers[account.Email];

            Console.WriteLine(
                $"{account.Role}: {account.Email}, " +
                $"PublicId: {user.PublicId}");
        }

        Console.WriteLine(
            "Созданы или проверены 6 команд и 12 участников.");
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> users,
        TestAccount account,
        string password)
    {
        var existing = await users.FindByEmailAsync(
            account.Email);

        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(
                existing, account.Role))
            {
                throw new InvalidOperationException(
                    $"Пользователь {account.Email} уже " +
                    $"существует без роли {account.Role}.");
            }

            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = account.Email,
            Email = account.Email,
            FirstName = account.FirstName,
            LastName = account.LastName,
            EmailConfirmed = true
        };

        var createResult = await users.CreateAsync(
            user, password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Ошибка создания {account.Email}: " +
                string.Join("; ",
                    createResult.Errors.Select(
                        x => x.Description)));
        }

        var roleResult = await users.AddToRoleAsync(
            user, account.Role);

        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(user);

            throw new InvalidOperationException(
                $"Не удалось назначить роль {account.Role} " +
                $"пользователю {account.Email}.");
        }

        return user;
    }
}
