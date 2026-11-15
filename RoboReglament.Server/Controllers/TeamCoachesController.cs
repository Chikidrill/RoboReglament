
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Coach)]
[Route("api/teams/{teamId:int}/coaches")]
public class TeamCoachesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeamCoachesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Получение тренеров команды.
    [HttpGet]
    public async Task<IActionResult> GetAll(int teamId)
    {
        var error = await CheckAccessAsync(teamId);

        if (error is not null)
            return error;

        var currentUserId = _userManager.GetUserId(User);

        var coaches = await _context.TeamCoaches
            .AsNoTracking()
            .Where(x => x.TeamId == teamId)
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .Select(x => new
            {
                x.User.PublicId,
                x.User.FirstName,
                x.User.LastName,
                x.User.MiddleName,
                IsSelf = x.UserId == currentUserId
            })
            .ToListAsync();

        return Ok(coaches);
    }

    // Поиск зарегистрированного тренера.
    [HttpGet("lookup/{publicId:long}")]
    public async Task<IActionResult> Lookup(
        int teamId,
        long publicId)
    {
        var error = await CheckAccessAsync(teamId);

        if (error is not null)
            return error;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PublicId == publicId);

        if (user is null ||
            !await _userManager.IsInRoleAsync(user, UserRoles.Coach))
        {
            return NotFound(new
            {
                message = "Пользователь с ролью тренера не найден."
            });
        }

        var alreadyInTeam = await _context.TeamCoaches
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.UserId == user.Id);

        return Ok(new
        {
            user.PublicId,
            user.FirstName,
            user.LastName,
            user.MiddleName,
            AlreadyInTeam = alreadyInTeam
        });
    }

    // Добавление другого тренера.
    [HttpPost]
    public async Task<IActionResult> Add(
        int teamId,
        [FromBody] AddTeamCoachRequest request)
    {
        var error = await CheckAccessAsync(teamId);

        if (error is not null)
            return error;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(x =>
                x.PublicId == request.PublicId);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Пользователь не найден."
            });
        }

        if (!await _userManager.IsInRoleAsync(
                user, UserRoles.Coach))
        {
            return BadRequest(new
            {
                message = "Пользователь не имеет роли тренера."
            });
        }

        var exists = await _context.TeamCoaches
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.UserId == user.Id);

        if (exists)
        {
            return Conflict(new
            {
                message = "Пользователь уже является тренером команды."
            });
        }

        _context.TeamCoaches.Add(new TeamCoach
        {
            TeamId = teamId,
            UserId = user.Id
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sql &&
                  (sql.Number == 2601 || sql.Number == 2627))
        {
            return Conflict(new
            {
                message = "Тренер уже добавлен в команду."
            });
        }

        return NoContent();
    }

    // Исключение другого тренера.
    [HttpDelete("{publicId:long}")]
    public async Task<IActionResult> Remove(
        int teamId,
        long publicId)
    {
        try
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var error = await CheckAccessAsync(teamId);

            if (error is not null)
                return error;

            var currentUserId = _userManager.GetUserId(User);

            var membership = await _context.TeamCoaches
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.TeamId == teamId &&
                    x.User.PublicId == publicId);

            if (membership is null)
            {
                return NotFound(new
                {
                    message = "Тренер не найден в составе команды."
                });
            }

            if (membership.UserId == currentUserId)
            {
                return BadRequest(new
                {
                    message = "Нельзя исключить самого себя."
                });
            }

            var coachesCount = await _context.TeamCoaches
                .CountAsync(x => x.TeamId == teamId);

            if (coachesCount <= 1)
            {
                return Conflict(new
                {
                    message = "Нельзя исключить последнего тренера."
                });
            }

            _context.TeamCoaches.Remove(membership);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            return Conflict(new
            {
                message = "Состав команды изменился. Повторите операцию."
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sql &&
                  sql.Number == 1205)
        {
            return Conflict(new
            {
                message = "Состав команды изменился. Повторите операцию."
            });
        }
    }

    private async Task<IActionResult?> CheckAccessAsync(int teamId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var exists = await _context.Teams
            .AnyAsync(x => x.Id == teamId);

        if (!exists)
        {
            return NotFound(new
            {
                message = "Команда не найдена."
            });
        }

        var canManage = await _context.TeamCoaches
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.UserId == userId);

        return canManage ? null : Forbid();
    }
}

public sealed record AddTeamCoachRequest(long PublicId);
