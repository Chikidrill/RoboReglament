
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Organizer)]
[Route(
    "api/tournaments/{tournamentId:int}/" +
    "disciplines/{disciplineId:int}/" +
    "matches/{matchId:int}/protocols")]
public sealed class JudgeProtocolReviewController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public JudgeProtocolReviewController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Просмотр протоколов и расхождений судей.
    [HttpGet("review")]
    public async Task<IActionResult> Review(
        int tournamentId,
        int disciplineId,
        int matchId)
    {
        var match = await FindOwnedMatchAsync(
            tournamentId, disciplineId, matchId);

        if (match is null)
            return NotFound(new { message = "Выступление недоступно." });

        var sideIds = match.MatchGroups
            .Select(x => x.Id)
            .ToArray();

        var criteria = match.ProtocolTemplateId.HasValue
            ? await _db.ProtocolCriteria
                .AsNoTracking()
                .Where(x =>
                    x.ProtocolTemplateId ==
                    match.ProtocolTemplateId.Value)
                .OrderBy(x => x.Order)
                .ToListAsync()
            : new List<ProtocolCriterion>();

        var protocols = await _db.JudgeProtocols
            .AsNoTracking()
            .Include(x => x.Scores)
            .Where(x => sideIds.Contains(x.MatchGroupId))
            .ToListAsync();

        var sides = match.MatchGroups
            .OrderBy(x => x.Number)
            .Select(side =>
            {
                var sideProtocols = protocols
                    .Where(x => x.MatchGroupId == side.Id)
                    .ToList();

                var comparison = criteria.Select(criterion =>
                {
                    var values = match.Judges
                        .OrderBy(x => x.Judge.PublicId)
                        .Select(judge =>
                        {
                            var protocol = sideProtocols
                                .FirstOrDefault(x =>
                                    x.JudgeId == judge.JudgeId);

                            var score = protocol?.Scores
                                .FirstOrDefault(x =>
                                    x.ProtocolCriterionId == criterion.Id);

                            return new
                            {
                                JudgePublicId = judge.Judge.PublicId,
                                ProtocolId = (int?)protocol?.Id,
                                Status = protocol?.Status.ToString()
                                    ?? "Missing",
                                Value = (decimal?)score?.Value
                            };
                        })
                        .ToList();

                    var submittedValues = values
                        .Where(x => x.Status == "Submitted")
                        .Select(x => x.Value)
                        .Distinct()
                        .ToList();

                    return new
                    {
                        CriterionId = criterion.Id,
                        CriterionName = criterion.Name,
                        HasMismatch = submittedValues.Count > 1,
                        Values = values
                    };
                }).ToList();

                var allSubmitted = match.Judges.Count > 0 &&
                    match.Judges.All(judge =>
                        sideProtocols.Any(protocol =>
                            protocol.JudgeId == judge.JudgeId &&
                            protocol.Status ==
                                JudgeProtocolStatus.Submitted));

                var hasMismatch = comparison.Any(x => x.HasMismatch);

                return new
                {
                    SideId = side.Id,
                    side.Number,
                    side.Name,
                    Status = hasMismatch
                        ? "Mismatch"
                        : allSubmitted ? "Ready" : "Pending",
                    Criteria = comparison
                };
            })
            .ToList();

        return Ok(new
        {
            MatchId = match.Id,
            AggregationMethod =
                match.Discipline.JudgeAggregationMethod,
            Sides = sides
        });
    }

    // Возврат отправленного протокола на исправление.
    [HttpPost("{protocolId:int}/reopen")]
    public async Task<IActionResult> Reopen(
        int tournamentId,
        int disciplineId,
        int matchId,
        int protocolId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var match = await FindOwnedMatchAsync(
            tournamentId, disciplineId, matchId);

        if (match is null)
            return NotFound(new { message = "Выступление недоступно." });

        if (match.Status != MatchStatus.InProgress ||
            match.Tournament.Status != TournamentStatus.InProgress)
        {
            return Conflict(new
            {
                message =
                    "Возврат протокола доступен только " +
                    "во время проведения выступления."
            });
        }

        var assignedJudgeIds = match.Judges
            .Select(x => x.JudgeId)
            .ToArray();

        var protocol = await _db.JudgeProtocols
            .FirstOrDefaultAsync(x =>
                x.Id == protocolId &&
                x.MatchGroup.MatchId == matchId &&
                x.JudgeId != null &&
                assignedJudgeIds.Contains(x.JudgeId));

        if (protocol is null)
        {
            return NotFound(new
            {
                message = "Протокол не найден."
            });
        }

        if (protocol.Status != JudgeProtocolStatus.Submitted)
        {
            return Conflict(new
            {
                message = "Протокол не находится в статусе Submitted."
            });
        }

        // Оценки остаются неизменными.
        // Их сможет скорректировать только сам судья.
        protocol.Status = JudgeProtocolStatus.Draft;
        protocol.SubmittedAt = null;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            protocol.Id,
            protocol.MatchGroupId,
            protocol.JudgeId,
            protocol.Status
        });
    }

    private Task<Match?> FindOwnedMatchAsync(
        int tournamentId,
        int disciplineId,
        int matchId)
    {
        var userId = _users.GetUserId(User);

        return _db.Matches
            .Include(x => x.Tournament)
            .Include(x => x.Discipline)
            .Include(x => x.MatchGroups)
            .Include(x => x.Judges)
                .ThenInclude(x => x.Judge)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Tournament.Organizers.Any(o =>
                    o.UserId == userId));
    }
}
