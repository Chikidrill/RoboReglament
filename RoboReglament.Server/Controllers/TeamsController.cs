using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Teams;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeamsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /api/teams
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeamResponse>>> GetAll()
    {
        var teams = await _context.Teams
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TeamResponse
            {
                Id = x.Id,
                Name = x.Name,
                Organization = x.Organization,
                City = x.City,
                ParticipantsCount = x.Participants.Count
            })
            .ToListAsync();

        return Ok(teams);
    }

    // GET: /api/teams/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeamResponse>> GetById(int id)
    {
        var team = await _context.Teams
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TeamResponse
            {
                Id = x.Id,
                Name = x.Name,
                Organization = x.Organization,
                City = x.City,
                ParticipantsCount = x.Participants.Count
            })
            .FirstOrDefaultAsync();

        if (team is null)
        {
            return NotFound(new
            {
                message = "Команда не найдена."
            });
        }

        return Ok(team);
    }

    // POST: /api/teams
    [Authorize(Roles = UserRoles.Coach)]
    [HttpPost]
    public async Task<ActionResult<TeamResponse>> Create(
        CreateTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Название команды не может быть пустым."
            });
        }

        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Unauthorized();
        }

        var team = new Team
        {
            Name = request.Name.Trim(),
            Organization = request.Organization?.Trim(),
            City = request.City?.Trim()
        };

        // Создатель команды автоматически становится её тренером.
        team.Coaches.Add(new TeamCoach
        {
            UserId = userId
        });

        _context.Teams.Add(team);

        await _context.SaveChangesAsync();

        var response = new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Organization = team.Organization,
            City = team.City,
            ParticipantsCount = 0
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = team.Id },
            response);
    }

    // PUT: /api/teams/5
    [Authorize(Roles = UserRoles.Coach)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TeamResponse>> Update(
        int id,
        UpdateTeamRequest request)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Unauthorized();
        }

        var team = await _context.Teams
            .FirstOrDefaultAsync(x => x.Id == id);

        if (team is null)
        {
            return NotFound(new
            {
                message = "Команда не найдена."
            });
        }

        // Одной роли Coach недостаточно.
        // Пользователь должен быть тренером именно этой команды.
        var canManage = await _context.TeamCoaches
            .AnyAsync(x =>
                x.TeamId == id &&
                x.UserId == userId);

        if (!canManage)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Название команды не может быть пустым."
            });
        }

        team.Name = request.Name.Trim();
        team.Organization = request.Organization?.Trim();
        team.City = request.City?.Trim();

        await _context.SaveChangesAsync();

        var participantsCount = await _context.Participants
            .CountAsync(x => x.TeamId == id);

        var response = new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Organization = team.Organization,
            City = team.City,
            ParticipantsCount = participantsCount
        };

        return Ok(response);
    }

    // DELETE: /api/teams/5
    [Authorize(Roles = UserRoles.Coach)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Unauthorized();
        }

        var team = await _context.Teams
            .FirstOrDefaultAsync(x => x.Id == id);

        if (team is null)
        {
            return NotFound(new
            {
                message = "Команда не найдена."
            });
        }

        var canManage = await _context.TeamCoaches
            .AnyAsync(x =>
                x.TeamId == id &&
                x.UserId == userId);

        if (!canManage)
        {
            return Forbid();
        }

        var hasApplications = await _context.TournamentApplications
            .AnyAsync(x => x.TeamId == id);

        var hasMatches = await _context.MatchTeams
            .AnyAsync(x => x.TeamId == id);

        if (hasApplications || hasMatches)
        {
            return Conflict(new
            {
                message =
                    "Нельзя удалить команду, которая уже участвовала в турнирах."
            });
        }

        _context.Teams.Remove(team);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}