
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
[Authorize(Roles = UserRoles.Organizer)]
[Route("api/tournaments/{tournamentId:int}/disciplines")]
public class DisciplinesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DisciplinesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int tournamentId,
        DisciplineRequest request)
    {
        var error = await CheckAccessAsync(tournamentId);

        if (error is not null)
            return error;

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Укажите название." });

        var discipline = new Discipline
        {
            TournamentId = tournamentId,
            Name = name,
            Description = request.Description?.Trim()
        };

        _context.Disciplines.Add(discipline);

        if (!await SaveAsync())
            return DuplicateName();

        return StatusCode(201, ToResponse(discipline));
    }

    [HttpPut("{disciplineId:int}")]
    public async Task<IActionResult> Update(
        int tournamentId,
        int disciplineId,
        DisciplineRequest request)
    {
        var error = await CheckAccessAsync(tournamentId);

        if (error is not null)
            return error;

        var discipline = await _context.Disciplines
            .FirstOrDefaultAsync(x =>
                x.Id == disciplineId &&
                x.TournamentId == tournamentId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина не найдена." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Укажите название." });

        discipline.Name = request.Name.Trim();
        discipline.Description = request.Description?.Trim();

        if (!await SaveAsync())
            return DuplicateName();

        return Ok(ToResponse(discipline));
    }

    [HttpDelete("{disciplineId:int}")]
    public async Task<IActionResult> Delete(
        int tournamentId,
        int disciplineId)
    {
        var error = await CheckAccessAsync(tournamentId);

        if (error is not null)
            return error;

        var discipline = await _context.Disciplines
            .FirstOrDefaultAsync(x =>
                x.Id == disciplineId &&
                x.TournamentId == tournamentId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина не найдена." });

        var hasRelations =
            await _context.TournamentApplications
                .AnyAsync(x => x.DisciplineId == disciplineId) ||
            await _context.Matches
                .AnyAsync(x => x.DisciplineId == disciplineId) ||
            await _context.ProtocolTemplates
                .AnyAsync(x => x.DisciplineId == disciplineId);

        if (hasRelations)
        {
            return Conflict(new
            {
                message = "Дисциплина содержит связанные данные."
            });
        }

        _context.Disciplines.Remove(discipline);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<IActionResult?> CheckAccessAsync(int tournamentId)
    {
        var userId = _userManager.GetUserId(User);

        var tournament = await _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tournamentId &&
                x.Organizers.Any(o => o.UserId == userId));

        if (tournament is null)
        {
            return NotFound(new
            {
                message = "Турнир не найден или недоступен."
            });
        }

        if (tournament.Status != TournamentStatus.Draft)
        {
            return Conflict(new
            {
                message = "Дисциплины можно изменять только до открытия регистрации."
            });
        }

        return null;
    }

    private async Task<bool> SaveAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sql &&
                 (sql.Number == 2601 || sql.Number == 2627))
        {
            return false;
        }
    }

    private IActionResult DuplicateName()
    {
        return Conflict(new
        {
            message = "Дисциплина с таким названием уже существует."
        });
    }

    private static DisciplineResponse ToResponse(Discipline discipline)
    {
        return new DisciplineResponse
        {
            Id = discipline.Id,
            TournamentId = discipline.TournamentId,
            Name = discipline.Name,
            Description = discipline.Description
        };
    }
}
