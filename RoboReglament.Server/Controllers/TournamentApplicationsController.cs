
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.TournamentApplications;
using RoboReglament.Server.DTOs.TournamentsApllications;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId:int}/applications")]
[Authorize]
public class TournamentApplicationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TournamentApplicationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // POST: подать заявку
    [HttpPost]
    [Authorize(Roles = UserRoles.Coach)]
    public async Task<IActionResult> Create(
        int tournamentId,
        [FromBody] CreateTournamentApplicationRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (tournament is null)
            return NotFound(new { message = "Турнир не найден." });

        if (tournament.Status != TournamentStatus.Registration)
            return Conflict(new { message = "Регистрация не открыта." });

        if (tournament.RegistrationDeadline.HasValue &&
            DateTime.UtcNow > tournament.RegistrationDeadline.Value)
        {
            return Conflict(new
            {
                message = "Срок подачи заявок истёк."
            });
        }

        var team = await _context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TeamId);

        if (team is null)
            return NotFound(new { message = "Команда не найдена." });

        if (!await IsTeamCoachAsync(request.TeamId, userId))
            return Forbid();

        var exists = await _context.TournamentApplications
            .AnyAsync(x =>
                x.TournamentId == tournamentId &&
                x.TeamId == request.TeamId);

        if (exists)
            return Conflict(new { message = "Заявка уже существует." });

        var participants = await _context.Participants
            .AsNoTracking()
            .Where(x => x.TeamId == request.TeamId)
            .ToListAsync();

        if (participants.Count == 0)
        {
            return BadRequest(new
            {
                message = "В команде нет участников."
            });
        }

        var application = new TournamentApplication
        {
            TournamentId = tournamentId,
            TeamId = request.TeamId,
            Status = ApplicationStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            DeclaredParticipants = CreateSnapshots(participants)
        };

        _context.TournamentApplications.Add(application);

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
                message = "Команда уже зарегистрирована на турнир."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { tournamentId, applicationId = application.Id },
            ToResponse(application, team.Name));
    }

    // GET: список доступных заявок
    [HttpGet]
    public async Task<IActionResult> GetAll(int tournamentId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var exists = await _context.Tournaments
            .AnyAsync(x => x.Id == tournamentId);

        if (!exists)
            return NotFound(new { message = "Турнир не найден." });

        var canViewAll = await CanViewAllAsync(tournamentId, userId);

        if (!canViewAll && !User.IsInRole(UserRoles.Coach))
            return Forbid();

        var applications = await AccessibleApplications(
                tournamentId, userId, canViewAll)
            .Include(x => x.Team)
            .Include(x => x.DeclaredParticipants)
            .Include(x => x.Accreditation)
            .OrderByDescending(x => x.SubmittedAt)
            .ToListAsync();

        return Ok(applications.Select(x =>
            ToResponse(x, x.Team.Name)).ToList());
    }

    // GET: конкретная заявка
    [HttpGet("{applicationId:int}")]
    public async Task<IActionResult> GetById(
        int tournamentId,
        int applicationId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var canViewAll = await CanViewAllAsync(tournamentId, userId);

        if (!canViewAll && !User.IsInRole(UserRoles.Coach))
            return Forbid();

        var application = await AccessibleApplications(
                tournamentId, userId, canViewAll)
            .Include(x => x.Team)
            .Include(x => x.DeclaredParticipants)
            .Include(x => x.Accreditation)
            .FirstOrDefaultAsync(x => x.Id == applicationId);

        if (application is null)
            return NotFound(new { message = "Заявка не найдена." });

        return Ok(ToResponse(application, application.Team.Name));
    }

    // PUT: одобрить или отклонить заявку
    [HttpPut("{applicationId:int}/status")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> UpdateStatus(
        int tournamentId,
        int applicationId,
        [FromBody] UpdateApplicationStatusRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (tournament is null)
            return NotFound(new { message = "Турнир не найден." });

        if (!await IsTournamentOrganizerAsync(tournamentId, userId))
            return Forbid();

        if (tournament.Status != TournamentStatus.Registration)
        {
            return Conflict(new
            {
                message = "Рассмотрение заявок сейчас недоступно."
            });
        }

        if (request.Status != ApplicationStatus.Approved &&
            request.Status != ApplicationStatus.Rejected)
        {
            return BadRequest(new
            {
                message = "Допустимы только Approved и Rejected."
            });
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        if (request.Status == ApplicationStatus.Rejected &&
            comment is null)
        {
            return BadRequest(new
            {
                message = "Укажите причину отклонения."
            });
        }

        var exists = await _context.TournamentApplications
            .AnyAsync(x =>
                x.Id == applicationId &&
                x.TournamentId == tournamentId);

        if (!exists)
            return NotFound(new { message = "Заявка не найдена." });

        // Обновляем только заявку со статусом Pending.
        var updated = await _context.TournamentApplications
            .Where(x =>
                x.Id == applicationId &&
                x.TournamentId == tournamentId &&
                x.Status == ApplicationStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, request.Status)
                .SetProperty(x => x.Comment, comment));

        if (updated == 0)
        {
            return Conflict(new
            {
                message = "Заявка уже рассмотрена."
            });
        }

        var application = await _context.TournamentApplications
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.DeclaredParticipants)
            .Include(x => x.Accreditation)
            .FirstAsync(x => x.Id == applicationId);

        return Ok(ToResponse(application, application.Team.Name));
    }

    // PUT: обновить заявленный состав из текущего состава команды
    [HttpPut("{applicationId:int}/participants/refresh")]
    [Authorize(Roles = UserRoles.Coach)]
    public async Task<IActionResult> RefreshParticipants(
        int tournamentId,
        int applicationId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (tournament is null)
            return NotFound(new { message = "Турнир не найден." });

        if (tournament.Status != TournamentStatus.Registration)
        {
            return Conflict(new
            {
                message = "Изменение состава доступно " +
                          "только на этапе регистрации."
            });
        }

        var application = await _context.TournamentApplications
            .Include(x => x.Team)
            .Include(x => x.Accreditation)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.TournamentId == tournamentId);

        if (application is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (!await IsTeamCoachAsync(application.TeamId, userId))
            return Forbid();

        if (application.Accreditation?.IsAccredited == true)
        {
            return Conflict(new
            {
                message = "Команда уже аккредитована. Состав зафиксирован."
            });
        }

        var participants = await _context.Participants
            .AsNoTracking()
            .Where(x => x.TeamId == application.TeamId)
            .ToListAsync();

        if (participants.Count == 0)
        {
            return BadRequest(new
            {
                message = "Нельзя обновить заявку пустым составом."
            });
        }

        // Удаление старых копий и сохранение новых
        // выполняются в одной транзакции.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await _context.TournamentApplicationParticipants
            .Where(x => x.TournamentApplicationId == application.Id)
            .ExecuteDeleteAsync();

        application.DeclaredParticipants = CreateSnapshots(participants);
        application.Status = ApplicationStatus.Pending;
        application.Comment = null;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(ToResponse(application, application.Team.Name));
    }

    // POST: аккредитовать команду и зафиксировать состав
    [HttpPost("{applicationId:int}/accreditation")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Accredit(
        int tournamentId,
        int applicationId,
        [FromBody] AccreditApplicationRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (tournament is null)
            return NotFound(new { message = "Турнир не найден." });

        if (!await IsTournamentOrganizerAsync(tournamentId, userId))
            return Forbid();

        if (tournament.Status != TournamentStatus.Registration &&
            tournament.Status != TournamentStatus.InProgress)
        {
            return Conflict(new
            {
                message = "Аккредитация сейчас недоступна."
            });
        }

        var application = await _context.TournamentApplications
            .Include(x => x.Team)
            .Include(x => x.DeclaredParticipants)
            .Include(x => x.Accreditation)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.TournamentId == tournamentId);

        if (application is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (application.Status != ApplicationStatus.Approved)
        {
            return Conflict(new
            {
                message = "Аккредитовать можно только одобренную заявку."
            });
        }

        if (application.Accreditation?.IsAccredited == true)
        {
            return Conflict(new
            {
                message = "Команда уже аккредитована."
            });
        }

        if (application.DeclaredParticipants.Count == 0)
        {
            return Conflict(new
            {
                message = "В заявке отсутствуют участники."
            });
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        if (application.Accreditation is null)
        {
            application.Accreditation = new Accreditation
            {
                ApplicationId = application.Id,
                IsAccredited = true,
                AccreditedAt = DateTime.UtcNow,
                Comment = comment
            };
        }
        else
        {
            application.Accreditation.IsAccredited = true;
            application.Accreditation.AccreditedAt = DateTime.UtcNow;
            application.Accreditation.Comment = comment;
        }

        await _context.SaveChangesAsync();

        return Ok(ToResponse(application, application.Team.Name));
    }

    // Вспомогательные методы

    private Task<bool> IsTeamCoachAsync(int teamId, string userId)
    {
        return _context.TeamCoaches.AnyAsync(x =>
            x.TeamId == teamId && x.UserId == userId);
    }

    private Task<bool> IsTournamentOrganizerAsync(
        int tournamentId, string userId)
    {
        return _context.TournamentOrganizers.AnyAsync(x =>
            x.TournamentId == tournamentId && x.UserId == userId);
    }

    private async Task<bool> CanViewAllAsync(
        int tournamentId, string userId)
    {
        if (User.IsInRole(UserRoles.Administrator))
            return true;

        if (!User.IsInRole(UserRoles.Organizer))
            return false;

        return await IsTournamentOrganizerAsync(tournamentId, userId);
    }

    private IQueryable<TournamentApplication> AccessibleApplications(
        int tournamentId,
        string userId,
        bool canViewAll)
    {
        var query = _context.TournamentApplications
            .AsNoTracking()
            .Where(x => x.TournamentId == tournamentId);

        if (!canViewAll)
        {
            query = query.Where(x =>
                _context.TeamCoaches.Any(coach =>
                    coach.TeamId == x.TeamId &&
                    coach.UserId == userId));
        }

        return query;
    }

    private static List<TournamentApplicationParticipant> CreateSnapshots(
        IEnumerable<Participant> participants)
    {
        return participants.Select(x =>
            new TournamentApplicationParticipant
            {
                ParticipantId = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                MiddleName = x.MiddleName,
                BirthDate = x.BirthDate
            }).ToList();
    }

    private static TournamentApplicationResponse ToResponse(
        TournamentApplication application,
        string teamName)
    {
        return new TournamentApplicationResponse
        {
            Id = application.Id,
            TournamentId = application.TournamentId,
            TeamId = application.TeamId,
            TeamName = teamName,
            Status = application.Status.ToString(),
            SubmittedAt = application.SubmittedAt,
            Comment = application.Comment,

            IsAccredited =
                application.Accreditation?.IsAccredited ?? false,
            AccreditedAt =
                application.Accreditation?.AccreditedAt,
            AccreditationComment =
                application.Accreditation?.Comment,

            Participants = application.DeclaredParticipants
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => new ApplicationParticipantResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    MiddleName = x.MiddleName,
                    BirthDate = x.BirthDate
                })
                .ToList()
        };
    }
}
