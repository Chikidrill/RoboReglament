
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Participants;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Participant)]
[Route("api/participant/overview")]
public sealed class ParticipantOverviewController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ParticipantOverviewController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<ParticipantOverviewDto>> Get()
    {
        var userId = _users.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        // Команды, в которых пользователь
        // действительно числится участником.
        var teams = await _db.Participants
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Team.Name)
            .Select(x => new ParticipantTeamItem(
                x.TeamId,
                x.Team.Name,
                x.Team.Organization
            ))
            .ToListAsync();

        // Заявки, в состав которых включён
        // непосредственно этот участник.
        var applications = await _db
            .TournamentApplicationParticipants
            .AsNoTracking()
            .Where(x =>
                x.Participant != null &&
                x.Participant.UserId == userId)
            .Select(x => new ParticipantApplicationItem(
                x.TournamentApplicationId,
                x.TournamentApplication.TournamentId,
                x.TournamentApplication.Tournament.Name,
                x.TournamentApplication.Tournament.Status,
                x.TournamentApplication.DisciplineId,
                x.TournamentApplication.Discipline.Name,
                x.TournamentApplication.TeamId,
                x.TournamentApplication.Team.Name,
                x.TournamentApplication.Status,
                x.TournamentApplication.Accreditation != null &&
                x.TournamentApplication.Accreditation.IsAccredited
            ))
            .ToListAsync();

        // Матчи показываем только по тем заявкам,
        // в которых участник присутствует и команда
        // допущена к соревнованию.
        var scopes = applications
            .Where(x =>
                x.ApplicationStatus == ApplicationStatus.Approved &&
                x.IsAccredited)
            .Select(x => (
                x.TournamentId,
                x.DisciplineId,
                x.TeamId))
            .ToHashSet();

        var teamIds = scopes
            .Select(x => x.TeamId)
            .Distinct()
            .ToArray();

        var candidates = await _db.MatchTeams
            .AsNoTracking()
            .Where(x => teamIds.Contains(x.TeamId))
            .Select(x => new ParticipantMatchItem(
                x.MatchGroup.MatchId,
                x.MatchGroup.Match.TournamentId,
                x.MatchGroup.Match.DisciplineId,
                x.TeamId,
                x.MatchGroup.Match.Name,
                x.MatchGroup.Match.Number,
                x.MatchGroup.Match.ScheduledAt,
                x.MatchGroup.Match.Status
            ))
            .ToListAsync();

        var matches = candidates
            .Where(x => scopes.Contains((
                x.TournamentId,
                x.DisciplineId,
                x.TeamId)))
            .OrderBy(x => x.ScheduledAt)
            .ThenBy(x => x.Number)
            .ToList();

        return Ok(new ParticipantOverviewDto(
            teams,
            applications,
            matches
        ));
    }
}
