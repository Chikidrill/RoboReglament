
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Tournaments;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/tournaments")]
public class TournamentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TournamentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Публичный список опубликованных турниров.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tournaments = await _context.Tournaments
            .AsNoTracking()
            .Include(x => x.Disciplines)
            .Where(x => x.Status != TournamentStatus.Draft)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        return Ok(tournaments.Select(ToResponse));
    }

    // Турниры текущего организатора.
    [HttpGet("mine")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> GetMine()
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var tournaments = await _context.Tournaments
            .AsNoTracking()
            .Include(x => x.Disciplines)
            .Where(x => x.Organizers.Any(o =>
                o.UserId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(tournaments.Select(ToResponse));
    }

    // Карточка турнира.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tournament = await _context.Tournaments
            .AsNoTracking()
            .Include(x => x.Disciplines)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tournament is null)
            return TournamentNotFound();

        // Черновик доступен только его организаторам.
        if (tournament.Status == TournamentStatus.Draft)
        {
            var userId = _userManager.GetUserId(User);

            var canView = User.IsInRole(UserRoles.Organizer) &&
                userId is not null &&
                await _context.TournamentOrganizers.AnyAsync(x =>
                    x.TournamentId == id &&
                    x.UserId == userId);

            if (!canView)
                return TournamentNotFound();
        }

        return Ok(ToResponse(tournament));
    }

    // Создание турнира.
    [HttpPost]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Create(
        CreateTournamentRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var deadline = NormalizeUtc(request.RegistrationDeadline);

        var validationError = ValidateTournament(
            request.Name,
            request.StartDate,
            request.EndDate,
            deadline);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Location = request.Location?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RegistrationDeadline = deadline,
            Status = TournamentStatus.Draft
        };

        tournament.Organizers.Add(new TournamentOrganizer
        {
            UserId = userId
        });

        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = tournament.Id },
            ToResponse(tournament));
    }

    // Редактирование турнира.
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Update(
        int id,
        UpdateTournamentRequest request)
    {
        var tournament = await FindOwnedAsync(id);

        if (tournament is null)
            return TournamentNotFound();

        if (tournament.Status == TournamentStatus.InProgress ||
            tournament.Status == TournamentStatus.Completed)
        {
            return Conflict(new
            {
                message = "Редактирование турнира уже недоступно."
            });
        }

        var deadline = NormalizeUtc(request.RegistrationDeadline);

        var validationError = ValidateTournament(
            request.Name,
            request.StartDate,
            request.EndDate,
            deadline);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        tournament.Name = request.Name.Trim();
        tournament.Description = request.Description?.Trim();
        tournament.Location = request.Location?.Trim();
        tournament.StartDate = request.StartDate;
        tournament.EndDate = request.EndDate;
        tournament.RegistrationDeadline = deadline;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(tournament));
    }

    // Удаление неиспользованного черновика.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Delete(int id)
    {
        var tournament = await FindOwnedAsync(id);

        if (tournament is null)
            return TournamentNotFound();

        if (tournament.Status != TournamentStatus.Draft)
        {
            return Conflict(new
            {
                message = "Удалить можно только черновик турнира."
            });
        }

        var hasRelations =
            await _context.TournamentApplications
                .AnyAsync(x => x.TournamentId == id) ||
            await _context.Matches
                .AnyAsync(x => x.TournamentId == id) ||
            await _context.ProtocolTemplates
                .AnyAsync(x => x.TournamentId == id);

        if (hasRelations)
        {
            return Conflict(new
            {
                message = "Турнир содержит связанные данные."
            });
        }

        _context.Tournaments.Remove(tournament);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Изменение этапа проведения турнира.
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateTournamentStatusRequest request)
    {
        var tournament = await FindOwnedAsync(id);

        if (tournament is null)
            return TournamentNotFound();

        var allowed = (tournament.Status, request.Status) switch
        {
            (TournamentStatus.Draft,
             TournamentStatus.Registration) => true,

            (TournamentStatus.Registration,
             TournamentStatus.InProgress) => true,

            (TournamentStatus.InProgress,
             TournamentStatus.Completed) => true,

            _ => false
        };

        if (!allowed)
        {
            return Conflict(new
            {
                message = "Недопустимое изменение статуса турнира."
            });
        }

        if (request.Status == TournamentStatus.Registration)
        {
            if (tournament.Disciplines.Count == 0)
            {
                return Conflict(new
                {
                    message = "Добавьте хотя бы одну дисциплину."
                });
            }

            if (tournament.RegistrationDeadline.HasValue &&
                tournament.RegistrationDeadline.Value <= DateTime.UtcNow)
            {
                return Conflict(new
                {
                    message = "Срок регистрации уже истёк."
                });
            }
        }

        if (request.Status == TournamentStatus.InProgress)
        {
            var hasPending = await _context.TournamentApplications
                .AnyAsync(x =>
                    x.TournamentId == id &&
                    x.Status == ApplicationStatus.Pending);

            if (hasPending)
            {
                return Conflict(new
                {
                    message = "Сначала рассмотрите все поданные заявки."
                });
            }
        }

        tournament.Status = request.Status;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(tournament));
    }

    // Получение организаторов турнира.
    [HttpGet("{id:int}/organizers")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> GetOrganizers(int id)
    {
        if (await FindOwnedAsync(id) is null)
            return TournamentNotFound();

        var organizers = await _context.TournamentOrganizers
            .AsNoTracking()
            .Where(x => x.TournamentId == id)
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .Select(x => new TournamentOrganizerResponse
            {
                Id = x.User.Id,
                PublicId = x.User.PublicId,
                Email = x.User.Email ?? string.Empty,
                FirstName = x.User.FirstName,
                LastName = x.User.LastName,
                MiddleName = x.User.MiddleName
            })
            .ToListAsync();

        return Ok(organizers);
    }

    // Поиск организатора по публичному ID.
    [HttpGet("{id:int}/organizers/lookup/{publicId:long}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> LookupOrganizer(
        int id,
        long publicId)
    {
        if (await FindOwnedAsync(id) is null)
            return TournamentNotFound();

        var user = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PublicId == publicId);

        if (user is null ||
            !await _userManager.IsInRoleAsync(
                user, UserRoles.Organizer))
        {
            return NotFound(new
            {
                message = "Пользователь с ролью организатора не найден."
            });
        }

        var alreadyOrganizer =
            await _context.TournamentOrganizers.AnyAsync(x =>
                x.TournamentId == id &&
                x.UserId == user.Id);

        return Ok(new
        {
            user.Id,
            user.PublicId,
            user.FirstName,
            user.LastName,
            user.MiddleName,
            AlreadyOrganizer = alreadyOrganizer
        });
    }

    // Добавление организатора.
    [HttpPost("{id:int}/organizers/{newOrganizerId}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> AddOrganizer(
        int id,
        string newOrganizerId)
    {
        var tournament = await FindOwnedAsync(id);

        if (tournament is null)
            return TournamentNotFound();

        if (tournament.Status == TournamentStatus.Completed)
            return Conflict(new { message = "Турнир завершён." });

        var user = await _userManager.FindByIdAsync(newOrganizerId);

        if (user is null ||
            !await _userManager.IsInRoleAsync(
                user, UserRoles.Organizer))
        {
            return NotFound(new
            {
                message = "Организатор не найден."
            });
        }

        var exists = await _context.TournamentOrganizers
            .AnyAsync(x =>
                x.TournamentId == id &&
                x.UserId == user.Id);

        if (exists)
        {
            return Conflict(new
            {
                message = "Пользователь уже является организатором."
            });
        }

        _context.TournamentOrganizers.Add(
            new TournamentOrganizer
            {
                TournamentId = id,
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
                message = "Организатор уже добавлен."
            });
        }

        return NoContent();
    }

    // Исключение организатора.
    [HttpDelete("{id:int}/organizers/{organizerId}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> RemoveOrganizer(
        int id,
        string organizerId)
    {
        try
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var tournament = await FindOwnedAsync(id);

            if (tournament is null)
                return TournamentNotFound();

            if (tournament.Status == TournamentStatus.Completed)
                return Conflict(new { message = "Турнир завершён." });

            if (organizerId == _userManager.GetUserId(User))
            {
                return BadRequest(new
                {
                    message = "Нельзя исключить самого себя."
                });
            }

            var organizer = await _context.TournamentOrganizers
                .FirstOrDefaultAsync(x =>
                    x.TournamentId == id &&
                    x.UserId == organizerId);

            if (organizer is null)
            {
                return NotFound(new
                {
                    message = "Организатор не найден в турнире."
                });
            }

            var count = await _context.TournamentOrganizers
                .CountAsync(x => x.TournamentId == id);

            if (count <= 1)
            {
                return Conflict(new
                {
                    message = "Нельзя исключить последнего организатора."
                });
            }

            _context.TournamentOrganizers.Remove(organizer);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            return Conflict(new
            {
                message = "Список организаторов изменился. Повторите запрос."
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sql &&
                  sql.Number == 1205)
        {
            return Conflict(new
            {
                message = "Список организаторов изменился. Повторите запрос."
            });
        }
    }

    private Task<Tournament?> FindOwnedAsync(int id)
    {
        var userId = _userManager.GetUserId(User);

        return _context.Tournaments
            .Include(x => x.Disciplines)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Organizers.Any(o => o.UserId == userId));
    }

    private IActionResult TournamentNotFound()
    {
        return NotFound(new
        {
            message = "Турнир не найден или недоступен."
        });
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.Kind == DateTimeKind.Local
            ? value.Value.ToUniversalTime()
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    private static string? ValidateTournament(
        string name,
        DateOnly startDate,
        DateOnly endDate,
        DateTime? deadline)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Укажите название турнира.";

        if (startDate == default || endDate == default)
            return "Укажите даты турнира.";

        if (startDate > endDate)
            return "Дата начала не может быть позже даты окончания.";

        if (deadline.HasValue &&
            DateOnly.FromDateTime(deadline.Value) > startDate)
        {
            return "Срок регистрации не может быть позже даты начала турнира.";
        }

        return null;
    }
    [HttpGet("{id:int}/manage")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> GetManagedTournament(int id)
    {
        var tournament = await FindOwnedAsync(id);

        if (tournament is null)
            return TournamentNotFound();

        return Ok(ToResponse(tournament));
    }
    private static TournamentResponse ToResponse(Tournament tournament)
    {
        return new TournamentResponse
        {
            Id = tournament.Id,
            Name = tournament.Name,
            Description = tournament.Description,
            Location = tournament.Location,
            StartDate = tournament.StartDate,
            EndDate = tournament.EndDate,
            RegistrationDeadline =
                NormalizeUtc(tournament.RegistrationDeadline),
            Status = tournament.Status,
            CreatedAt = DateTime.SpecifyKind(
                tournament.CreatedAt, DateTimeKind.Utc),

            Disciplines = tournament.Disciplines
                .OrderBy(x => x.Name)
                .Select(x => new DisciplineResponse
                {
                    Id = x.Id,
                    TournamentId = x.TournamentId,
                    Name = x.Name,
                    Description = x.Description
                })
                .ToList()
        };
    }
}
