
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Participants;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/teams/{teamId:int}/participants")]
[Authorize(Roles = UserRoles.Coach)]
public class ParticipantsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ParticipantsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Получение состава команды.
    [HttpGet]
    public async Task<IActionResult> GetAll(int teamId)
    {
        var error = await CheckTeamAccessAsync(teamId);

        if (error is not null)
            return error;

        var participants = await _context.Participants
            .AsNoTracking()
            .Where(x => x.TeamId == teamId)
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .Select(x => new ParticipantResponse
            {
                Id = x.Id,
                TeamId = x.TeamId,
                PublicId = x.User.PublicId,
                FirstName = x.User.FirstName,
                LastName = x.User.LastName,
                MiddleName = x.User.MiddleName,
                BirthDate = x.BirthDate
            })
            .ToListAsync();

        return Ok(participants);
    }

    // Получение участника команды.
    [HttpGet("{participantId:int}")]
    public async Task<IActionResult> GetById(
        int teamId,
        int participantId)
    {
        var error = await CheckTeamAccessAsync(teamId);

        if (error is not null)
            return error;

        var participant = await _context.Participants
            .AsNoTracking()
            .Where(x =>
                x.TeamId == teamId &&
                x.Id == participantId)
            .Select(x => new ParticipantResponse
            {
                Id = x.Id,
                TeamId = x.TeamId,
                PublicId = x.User.PublicId,
                FirstName = x.User.FirstName,
                LastName = x.User.LastName,
                MiddleName = x.User.MiddleName,
                BirthDate = x.BirthDate
            })
            .FirstOrDefaultAsync();

        if (participant is null)
        {
            return NotFound(new
            {
                message = "Участник команды не найден."
            });
        }

        return Ok(participant);
    }

    // Поиск зарегистрированного участника
    // по публичному ID.
    [HttpGet("lookup/{publicId:long}")]
    public async Task<IActionResult> Lookup(
        int teamId,
        long publicId)
    {
        var error = await CheckTeamAccessAsync(teamId);

        if (error is not null)
            return error;

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicId == publicId);

        if (user is null ||
            !await _userManager.IsInRoleAsync(
                user, UserRoles.Participant))
        {
            return NotFound(new
            {
                message = "Зарегистрированный участник не найден."
            });
        }

        var alreadyInTeam = await _context.Participants
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.UserId == user.Id);

        return Ok(new ParticipantLookupResponse
        {
            PublicId = user.PublicId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            AlreadyInTeam = alreadyInTeam
        });
    }

    // Добавление зарегистрированного участника
    // в команду по публичному ID.
    [HttpPost]
    public async Task<IActionResult> Create(
        int teamId,
        [FromBody] CreateParticipantRequest request)
    {
        var error = await CheckTeamAccessAsync(teamId);

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
            user, UserRoles.Participant))
        {
            return BadRequest(new
            {
                message =
                    "Пользователь не имеет роли участника."
            });
        }

        var alreadyExists = await _context.Participants
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.UserId == user.Id);

        if (alreadyExists)
        {
            return Conflict(new
            {
                message =
                    "Пользователь уже состоит в команде."
            });
        }

        var participant = new Participant
        {
            TeamId = teamId,

            // В БД сохраняем внутренний ID.
            UserId = user.Id,

            // Временно сохраняем поля совместимости.
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            BirthDate = null
        };

        _context.Participants.Add(participant);

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
                message =
                    "Пользователь уже состоит в команде."
            });
        }

        var response = new ParticipantResponse
        {
            Id = participant.Id,
            TeamId = participant.TeamId,
            PublicId = user.PublicId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            BirthDate = participant.BirthDate
        };

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                teamId,
                participantId = participant.Id
            },
            response);
    }

    // Исключение участника из команды.
    [HttpDelete("{participantId:int}")]
    public async Task<IActionResult> Delete(
        int teamId,
        int participantId)
    {
        var error = await CheckTeamAccessAsync(teamId);

        if (error is not null)
            return error;

        var participant = await _context.Participants
            .FirstOrDefaultAsync(x =>
                x.TeamId == teamId &&
                x.Id == participantId);

        if (participant is null)
        {
            return NotFound(new
            {
                message = "Участник команды не найден."
            });
        }

        // Сохраняем исторические снимки заявок,
        // убирая только связь с текущим членством.
        var applicationParticipants = await _context
            .TournamentApplicationParticipants
            .Where(x => x.ParticipantId == participantId)
            .ToListAsync();

        foreach (var item in applicationParticipants)
        {
            item.ParticipantId = null;
            item.Participant = null;
        }

        // Удаляется только членство в команде.
        // Аккаунт пользователя остаётся.
        _context.Participants.Remove(participant);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Проверка прав тренера на команду.
    private async Task<IActionResult?> CheckTeamAccessAsync(
        int teamId)
    {
        var teamExists = await _context.Teams
            .AnyAsync(x => x.Id == teamId);

        if (!teamExists)
        {
            return NotFound(new
            {
                message = "Команда не найдена."
            });
        }

        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var canManage = await _context.TeamCoaches
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.UserId == userId);

        if (!canManage)
            return Forbid();

        return null;
    }
}
