using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Tournaments;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TournamentResponse>>> GetAll()
    {
        var tournaments = await _context.Tournaments
            .AsNoTracking()
            .OrderByDescending(x => x.StartDate)
            .Select(x => new TournamentResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Location = x.Location,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                RegistrationDeadline = x.RegistrationDeadline,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(tournaments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TournamentResponse>> GetById(int id)
    {
        var tournament = await _context.Tournaments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TournamentResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Location = x.Location,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                RegistrationDeadline = x.RegistrationDeadline,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (tournament is null)
        {
            return NotFound(new
            {
                message = "Турнир не найден."
            });
        }

        return Ok(tournament);
    }

    [Authorize(Roles = UserRoles.Organizer)]
    [HttpPost]
    public async Task<ActionResult<TournamentResponse>> Create(
        CreateTournamentRequest request)
    {
        if (request.StartDate > request.EndDate)
        {
            return BadRequest(new
            {
                message = "Дата начала турнира не может быть позже даты окончания."
            });
        }

        if (request.RegistrationDeadline.HasValue &&
            request.RegistrationDeadline.Value.Date >
            request.StartDate.ToDateTime(TimeOnly.MinValue).Date)
        {
            return BadRequest(new
            {
                message = "Окончание регистрации не может быть позже даты начала турнира."
            });
        }

        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Unauthorized();
        }

        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Location = request.Location?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RegistrationDeadline = request.RegistrationDeadline,
            Status = TournamentStatus.Draft
        };

        tournament.Organizers.Add(new TournamentOrganizer
        {
            UserId = userId
        });

        _context.Tournaments.Add(tournament);

        await _context.SaveChangesAsync();

        var response = ToResponse(tournament);

        return CreatedAtAction(
            nameof(GetById),
            new { id = tournament.Id },
            response);
    }

    [Authorize(Roles = UserRoles.Organizer)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TournamentResponse>> Update(
        int id,
        UpdateTournamentRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Unauthorized();
        }

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tournament is null)
        {
            return NotFound(new
            {
                message = "Турнир не найден."
            });
        }

        var canManage = await _context.TournamentOrganizers
            .AnyAsync(x =>
                x.TournamentId == id &&
                x.UserId == userId);

        if (!canManage)
        {
            return Forbid();
        }

        if (request.StartDate > request.EndDate)
        {
            return BadRequest(new
            {
                message = "Дата начала турнира не может быть позже даты окончания."
            });
        }

        tournament.Name = request.Name.Trim();
        tournament.Description = request.Description?.Trim();
        tournament.Location = request.Location?.Trim();
        tournament.StartDate = request.StartDate;
        tournament.EndDate = request.EndDate;
        tournament.RegistrationDeadline =
            request.RegistrationDeadline;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(tournament));
    }

    [Authorize(Roles = UserRoles.Organizer)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Unauthorized();
        }

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tournament is null)
        {
            return NotFound(new
            {
                message = "Турнир не найден."
            });
        }

        var canManage = await _context.TournamentOrganizers
            .AnyAsync(x =>
                x.TournamentId == id &&
                x.UserId == userId);

        if (!canManage)
        {
            return Forbid();
        }

        _context.Tournaments.Remove(tournament);

        await _context.SaveChangesAsync();

        return NoContent();
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
            RegistrationDeadline = tournament.RegistrationDeadline,
            Status = tournament.Status,
            CreatedAt = tournament.CreatedAt
        };
    }
}