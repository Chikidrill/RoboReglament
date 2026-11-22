
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
[Authorize]
[Route("api/tournaments/{tournamentId:int}/applications")]
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

    // Создание черновика или отправленной заявки.
    [HttpPost]
    [Authorize(Roles = UserRoles.Coach)]
    public async Task<IActionResult> Create(
        int tournamentId,
        CreateTournamentApplicationRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await FindTournamentAsync(tournamentId);

        if (tournament is null)
            return NotFound(new { message = "Турнир не найден." });

        if (!RegistrationOpen(tournament))
            return RegistrationClosed();

        var discipline = await _context.Disciplines
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.DisciplineId &&
                x.TournamentId == tournamentId);

        if (discipline is null)
        {
            return NotFound(new
            {
                message = "Дисциплина не найдена в этом турнире."
            });
        }

        var team = await _context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TeamId);

        if (team is null)
            return NotFound(new { message = "Команда не найдена." });

        if (!await IsTeamCoachAsync(team.Id, userId))
            return Forbid();

        var exists = await _context.TournamentApplications
            .AnyAsync(x =>
                x.TeamId == team.Id &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == request.DisciplineId);

        if (exists)
        {
            return Conflict(new
            {
                message = "Заявка команды уже существует."
            });
        }

        var participants = await GetTeamParticipantsAsync(team.Id);

        if (!request.SaveAsDraft && participants.Count == 0)
        {
            return BadRequest(new
            {
                message = "В команде нет участников."
            });
        }

        var application = new TournamentApplication
        {
            TournamentId = tournamentId,
            TeamId = team.Id,
            DisciplineId = discipline.Id,

            Status = request.SaveAsDraft
                ? ApplicationStatus.Draft
                : ApplicationStatus.Pending,

            SubmittedAt = request.SaveAsDraft
                ? null
                : DateTime.UtcNow,

            DeclaredParticipants = CreateSnapshots(participants)
        };

        _context.TournamentApplications.Add(application);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (IsDuplicate(ex))
        {
            return Conflict(new
            {
                message = "Заявка команды уже существует."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                tournamentId,
                applicationId = application.Id
            },
            ToResponse(application, team.Name, discipline.Name));
    }

    // Все собственные заявки тренера.
    // Можно дополнительно отфильтровать по турниру.
    [HttpGet("~/api/applications/mine")]
    [Authorize(Roles = UserRoles.Coach)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int? tournamentId = null)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var query = _context.TournamentApplications
            .AsNoTracking()
            .Where(x => x.Team.Coaches.Any(c =>
                c.UserId == userId));

        if (tournamentId.HasValue)
        {
            query = query.Where(x =>
                x.TournamentId == tournamentId.Value);
        }

        var applications = await query
            .Include(x => x.Team)
            .Include(x => x.Discipline)
            .Include(x => x.Tournament)
            .Include(x => x.DeclaredParticipants)
            .Include(x => x.Accreditation)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return Ok(applications.Select(x => new
        {
            TournamentName = x.Tournament.Name,
            TournamentStatus = x.Tournament.Status.ToString(),
            x.Tournament.RegistrationDeadline,
            Application = ToResponse(x, x.Team.Name)
        }));
    }

    // Список заявок турнира.
    // Черновики не показываются организатору.
    [HttpGet]
    public async Task<IActionResult> GetAll(int tournamentId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        if (!await _context.Tournaments
                .AnyAsync(x => x.Id == tournamentId))
        {
            return NotFound(new { message = "Турнир не найден." });
        }

        var canViewAll = await CanViewAllAsync(tournamentId, userId);

        if (!canViewAll && !User.IsInRole(UserRoles.Coach))
            return Forbid();

        var query = _context.TournamentApplications
            .AsNoTracking()
            .Where(x => x.TournamentId == tournamentId);

        if (canViewAll)
        {
            query = query.Where(x =>
                x.Status != ApplicationStatus.Draft);
        }
        else
        {
            query = query.Where(x =>
                x.Team.Coaches.Any(c => c.UserId == userId));
        }

        var applications = await query
            .Include(x => x.Team)
            .Include(x => x.Discipline)
            .Include(x => x.DeclaredParticipants)
            .Include(x => x.Accreditation)
            .OrderByDescending(x => x.SubmittedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return Ok(applications.Select(x =>
            ToResponse(x, x.Team.Name)));
    }

    // Получение конкретной заявки.
    [HttpGet("{applicationId:int}")]
    public async Task<IActionResult> GetById(
        int tournamentId,
        int applicationId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var canViewAll = await CanViewAllAsync(tournamentId, userId);
        var isCoach = User.IsInRole(UserRoles.Coach);

        if (!canViewAll && !isCoach)
            return Forbid();

        var application = await _context.TournamentApplications
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Discipline)
            .Include(x => x.DeclaredParticipants)
            .Include(x => x.Accreditation)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.TournamentId == tournamentId &&
                (
                    (canViewAll &&
                     x.Status != ApplicationStatus.Draft) ||
                    (isCoach &&
                     x.Team.Coaches.Any(c => c.UserId == userId))
                ));

        if (application is null)
            return NotFound(new { message = "Заявка не найдена." });

        return Ok(ToResponse(application, application.Team.Name));
    }

    // Отправка черновика или повторная отправка
    // отозванной заявки.
    [HttpPost("{applicationId:int}/submit")]
    [Authorize(Roles = UserRoles.Coach)]
    public Task<IActionResult> Submit(
        int tournamentId,
        int applicationId)
    {
        return UpdateRosterAsync(
            tournamentId,
            applicationId,
            submit: true);
    }

    // Обновление снимка состава команды.
    [HttpPut("{applicationId:int}/participants/refresh")]
    [Authorize(Roles = UserRoles.Coach)]
    public Task<IActionResult> RefreshParticipants(
        int tournamentId,
        int applicationId)
    {
        return UpdateRosterAsync(
            tournamentId,
            applicationId,
            submit: false);
    }

    private async Task<IActionResult> UpdateRosterAsync(
        int tournamentId,
        int applicationId,
        bool submit)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await FindTournamentAsync(tournamentId);

        if (tournament is null)
            return NotFound(new { message = "Турнир не найден." });

        if (!RegistrationOpen(tournament))
            return RegistrationClosed();

        var application = await FindTrackedApplicationAsync(
            tournamentId, applicationId);

        if (application is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (!await IsTeamCoachAsync(application.TeamId, userId))
            return Forbid();

        if (application.Accreditation?.IsAccredited == true)
            return AccreditedConflict();

        if (submit &&
            application.Status != ApplicationStatus.Draft &&
            application.Status != ApplicationStatus.Withdrawn)
        {
            return Conflict(new
            {
                message = "Заявка уже отправлена."
            });
        }

        if (!submit &&
            application.Status == ApplicationStatus.Withdrawn)
        {
            return Conflict(new
            {
                message = "Сначала отправьте отозванную заявку повторно."
            });
        }

        var participants = await GetTeamParticipantsAsync(
            application.TeamId);

        var remainsDraft =
            !submit && application.Status == ApplicationStatus.Draft;

        if (!remainsDraft && participants.Count == 0)
        {
            return BadRequest(new
            {
                message = "Нельзя отправить заявку с пустым составом."
            });
        }

        try
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            application.Status = remainsDraft
                ? ApplicationStatus.Draft
                : ApplicationStatus.Pending;

            if (submit)
                application.SubmittedAt = DateTime.UtcNow;

            application.Comment = null;

            // Принудительно обновляем строку заявки, даже если
            // её статус не изменился. Это проверяет RowVersion.
            _context.Entry(application)
                .Property(x => x.Status)
                .IsModified = true;

            await _context.SaveChangesAsync();

            await _context.TournamentApplicationParticipants
                .Where(x =>
                    x.TournamentApplicationId == application.Id)
                .ExecuteDeleteAsync();

            application.DeclaredParticipants =
                CreateSnapshots(participants);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ToResponse(
                application,
                application.Team.Name));
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrentChange();
        }
    }

    // Отзыв отправленной заявки.
    [HttpPost("{applicationId:int}/withdraw")]
    [Authorize(Roles = UserRoles.Coach)]
    public async Task<IActionResult> Withdraw(
        int tournamentId,
        int applicationId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await FindTournamentAsync(tournamentId);

        if (tournament is null)
            return NotFound(new { message = "Турнир не найден." });

        if (!RegistrationOpen(tournament))
            return RegistrationClosed();

        var application = await FindTrackedApplicationAsync(
            tournamentId, applicationId);

        if (application is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (!await IsTeamCoachAsync(application.TeamId, userId))
            return Forbid();

        if (application.Accreditation?.IsAccredited == true)
            return AccreditedConflict();

        if (application.Status != ApplicationStatus.Pending &&
            application.Status != ApplicationStatus.Approved)
        {
            return Conflict(new
            {
                message = "Заявку нельзя отозвать в текущем статусе."
            });
        }

        await _context.Entry(application)
            .Collection(x => x.DeclaredParticipants)
            .LoadAsync();

        application.Status = ApplicationStatus.Withdrawn;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrentChange();
        }

        return Ok(ToResponse(
            application,
            application.Team.Name));
    }

    // Удаление черновика.
    // Отправленные заявки физически не удаляются.
    [HttpDelete("{applicationId:int}")]
    [Authorize(Roles = UserRoles.Coach)]
    public async Task<IActionResult> DeleteDraft(
        int tournamentId,
        int applicationId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var application = await FindTrackedApplicationAsync(
            tournamentId, applicationId);

        if (application is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (!await IsTeamCoachAsync(application.TeamId, userId))
            return Forbid();

        if (application.Status != ApplicationStatus.Draft ||
            application.Accreditation?.IsAccredited == true)
        {
            return Conflict(new
            {
                message = "Удалить можно только черновик."
            });
        }

        _context.TournamentApplications.Remove(application);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrentChange();
        }

        return NoContent();
    }

    // Рассмотрение заявки организатором.
    [HttpPut("{applicationId:int}/status")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> UpdateStatus(
        int tournamentId,
        int applicationId,
        UpdateApplicationStatusRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await FindTournamentAsync(tournamentId);

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

        var application = await FindTrackedApplicationAsync(
            tournamentId, applicationId);

        if (application is null ||
            application.Status == ApplicationStatus.Draft)
        {
            return NotFound(new { message = "Заявка не найдена." });
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            return Conflict(new
            {
                message = "Заявка уже рассмотрена или отозвана."
            });
        }

        await _context.Entry(application)
            .Collection(x => x.DeclaredParticipants)
            .LoadAsync();

        application.Status = request.Status;
        application.Comment = comment;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrentChange();
        }

        return Ok(ToResponse(
            application,
            application.Team.Name));
    }

    // Аккредитация команды.
    [HttpPost("{applicationId:int}/accreditation")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Accredit(
        int tournamentId,
        int applicationId,
        AccreditApplicationRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournament = await FindTournamentAsync(tournamentId);

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

        var application = await FindTrackedApplicationAsync(
            tournamentId, applicationId);

        if (application is null ||
            application.Status == ApplicationStatus.Draft)
        {
            return NotFound(new { message = "Заявка не найдена." });
        }

        if (application.Status != ApplicationStatus.Approved)
        {
            return Conflict(new
            {
                message = "Аккредитовать можно только одобренную заявку."
            });
        }

        if (application.Accreditation?.IsAccredited == true)
            return AccreditedConflict();

        await _context.Entry(application)
            .Collection(x => x.DeclaredParticipants)
            .LoadAsync();

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
                ApplicationId = application.Id
            };
        }

        application.Accreditation.IsAccredited = true;
        application.Accreditation.AccreditedAt = DateTime.UtcNow;
        application.Accreditation.Comment = comment;

        // Аккредитация тоже должна изменить RowVersion заявки.
        _context.Entry(application)
            .Property(x => x.Status)
            .IsModified = true;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrentChange();
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            return ConcurrentChange();
        }

        return Ok(ToResponse(
            application,
            application.Team.Name));
    }

    // Вспомогательные методы.

    private Task<Tournament?> FindTournamentAsync(int tournamentId)
    {
        return _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tournamentId);
    }

    private Task<TournamentApplication?> FindTrackedApplicationAsync(
        int tournamentId,
        int applicationId)
    {
        return _context.TournamentApplications
            .Include(x => x.Team)
            .Include(x => x.Discipline)
            .Include(x => x.Accreditation)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.TournamentId == tournamentId);
    }

    private Task<List<Participant>> GetTeamParticipantsAsync(
        int teamId)
    {
        return _context.Participants
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.TeamId == teamId)
            .ToListAsync();
    }

    private Task<bool> IsTeamCoachAsync(int teamId, string userId)
    {
        return _context.TeamCoaches.AnyAsync(x =>
            x.TeamId == teamId &&
            x.UserId == userId);
    }

    private Task<bool> IsTournamentOrganizerAsync(
        int tournamentId,
        string userId)
    {
        return _context.TournamentOrganizers.AnyAsync(x =>
            x.TournamentId == tournamentId &&
            x.UserId == userId);
    }

    private async Task<bool> CanViewAllAsync(
        int tournamentId,
        string userId)
    {
        if (User.IsInRole(UserRoles.Administrator))
            return true;

        return User.IsInRole(UserRoles.Organizer) &&
            await IsTournamentOrganizerAsync(tournamentId, userId);
    }

    private static bool RegistrationOpen(Tournament tournament)
    {
        return tournament.Status == TournamentStatus.Registration &&
            (
                !tournament.RegistrationDeadline.HasValue ||
                DateTime.UtcNow <= tournament.RegistrationDeadline.Value
            );
    }

    private static bool IsDuplicate(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627);
    }

    private IActionResult RegistrationClosed()
    {
        return Conflict(new
        {
            message = "Приём заявок закрыт."
        });
    }

    private IActionResult AccreditedConflict()
    {
        return Conflict(new
        {
            message = "Команда уже аккредитована. Состав зафиксирован."
        });
    }

    private IActionResult ConcurrentChange()
    {
        return Conflict(new
        {
            message = "Заявка была изменена. Обновите страницу."
        });
    }

    private static List<TournamentApplicationParticipant> CreateSnapshots(
        IEnumerable<Participant> participants)
    {
        return participants.Select(x =>
            new TournamentApplicationParticipant
            {
                ParticipantId = x.Id,
                FirstName = x.User.FirstName,
                LastName = x.User.LastName,
                MiddleName = x.User.MiddleName,
                BirthDate = x.BirthDate
            }).ToList();
    }

    private static TournamentApplicationResponse ToResponse(
        TournamentApplication application,
        string teamName, string? disciplineName = null)
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
            DisciplineId = application.DisciplineId,

            DisciplineName =
                disciplineName ?? application.Discipline.Name,

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
