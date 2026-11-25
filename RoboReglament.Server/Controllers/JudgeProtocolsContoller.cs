
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Judging;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;
using RoboReglament.Server.Services;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Judge)]
[Route("api/judge/matches/{matchId:int}")]
public sealed class JudgeProtocolsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public JudgeProtocolsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Получение критериев, сторон и своих оценок.
    [HttpGet("scorecard")]
    public async Task<IActionResult> GetScorecard(int matchId)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var match = await AssignedMatchAsync(matchId, userId);

        if (match is null)
            return NotFound(new { message = "Выступление недоступно." });

        var template = match.ProtocolTemplate;

        if (template is null || template.Criteria.Count == 0)
        {
            return Conflict(new
            {
                message = "Для выступления не настроен судейский протокол."
            });
        }

        var criteria = template.Criteria
            .OrderBy(x => x.Order)
            .ToList();

        var sideIds = match.MatchGroups
            .Select(x => x.Id)
            .ToArray();

        var protocols = await _db.JudgeProtocols
            .AsNoTracking()
            .Include(x => x.Scores)
            .Where(x =>
                x.JudgeId == userId &&
                sideIds.Contains(x.MatchGroupId))
            .ToListAsync();

        var protocolBySide = protocols
            .ToDictionary(x => x.MatchGroupId);

        var response = new JudgeMatchScorecardResponse
        {
            MatchId = match.Id,
            MatchName = match.Name,
            Status = match.Status,

            Format = match.Discipline.Format,
            ScoringMode = match.Discipline.ScoringMode,

            TemplateName = template.Name,

            Criteria = criteria.Select(x => new JudgeCriterionResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                MinValue = x.MinValue,
                MaxValue = x.MaxValue,
                Weight = x.Weight,
                IsPenalty = x.IsPenalty,
                Order = x.Order
            }).ToList(),

            Sides = match.MatchGroups
                .OrderBy(x => x.Number)
                .Select(side => new JudgeSideScorecardResponse
                {
                    Id = side.Id,
                    Number = side.Number,
                    Name = side.Name,

                    Teams = side.MatchTeams
                        .OrderBy(x => x.Position)
                        .Select(x => x.Team.Name)
                        .ToList(),

                    Protocol = protocolBySide.TryGetValue(
                        side.Id, out var protocol)
                            ? ToResponse(protocol, criteria)
                            : null
                })
                .ToList()
        };

        return Ok(response);
    }

    // Сохранение неполного или полного черновика.
    [HttpPut("sides/{sideId:int}/protocol")]
    public async Task<IActionResult> SaveDraft(
        int matchId,
        int sideId,
        SaveJudgeProtocolRequest request)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var match = await AssignedMatchAsync(matchId, userId);

        if (match is null)
            return NotFound(new { message = "Выступление недоступно." });

        if (!CanJudge(match))
        {
            return Conflict(new
            {
                message = "Выставлять оценки можно только во время выступления."
            });
        }

        if (!match.MatchGroups.Any(x => x.Id == sideId))
            return NotFound(new { message = "Сторона не найдена." });

        var criteria = match.ProtocolTemplate?.Criteria.ToList();

        if (criteria is null || criteria.Count == 0)
        {
            return Conflict(new
            {
                message = "Судейский протокол не настроен."
            });
        }

        var scores = request.Scores
            .Select(x => new ScoreEntry(
                x.CriterionId,
                x.Value!.Value))
            .ToList();

        var validationError = ProtocolScoreCalculator.Validate(
            criteria,
            scores,
            requireComplete: false);

        if (validationError is not null)
        {
            return BadRequest(new
            {
                message = validationError
            });
        }

        var protocol = await _db.JudgeProtocols
            .Include(x => x.Scores)
            .FirstOrDefaultAsync(x =>
                x.MatchGroupId == sideId &&
                x.JudgeId == userId);

        if (protocol?.Status == JudgeProtocolStatus.Submitted)
        {
            return Conflict(new
            {
                message = "Отправленный протокол нельзя изменять."
            });
        }

        if (protocol is null)
        {
            protocol = new JudgeProtocol
            {
                MatchGroupId = sideId,
                JudgeId = userId,
                Status = JudgeProtocolStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _db.JudgeProtocols.Add(protocol);
        }

        protocol.Comment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        var values = scores.ToDictionary(
            x => x.CriterionId,
            x => x.Value);

        // Обновляем существующие оценки и удаляем те,
        // которые пользователь убрал из черновика.
        foreach (var savedScore in protocol.Scores.ToList())
        {
            if (values.TryGetValue(
                savedScore.ProtocolCriterionId, out var value))
            {
                savedScore.Value = value;
            }
            else
            {
                _db.JudgeScores.Remove(savedScore);
            }
        }

        // Добавляем оценки по новым критериям.
        var existingIds = protocol.Scores
            .Select(x => x.ProtocolCriterionId)
            .ToHashSet();

        foreach (var score in scores)
        {
            if (existingIds.Contains(score.CriterionId))
                continue;

            protocol.Scores.Add(new JudgeScore
            {
                ProtocolCriterionId = score.CriterionId,
                Value = score.Value
            });
        }

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsSqlConflict(ex))
        {
            return Conflict(new
            {
                message = "Протокол изменился. Повторите сохранение."
            });
        }


        var savedProtocol = await _db.JudgeProtocols
            .AsNoTracking()
            .Include(x => x.Scores)
            .SingleAsync(x => x.Id == protocol.Id);

        return Ok(ToResponse(savedProtocol, criteria));

    }

    // Финальная отправка сохранённого протокола.
    [HttpPost("sides/{sideId:int}/protocol/submit")]
    public async Task<IActionResult> Submit(
        int matchId,
        int sideId)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var match = await AssignedMatchAsync(matchId, userId);

        if (match is null)
            return NotFound(new { message = "Выступление недоступно." });

        if (!CanJudge(match))
        {
            return Conflict(new
            {
                message = "Отправка протоколов сейчас недоступна."
            });
        }

        if (!match.MatchGroups.Any(x => x.Id == sideId))
            return NotFound(new { message = "Сторона не найдена." });

        var protocol = await _db.JudgeProtocols
            .Include(x => x.Scores)
            .FirstOrDefaultAsync(x =>
                x.MatchGroupId == sideId &&
                x.JudgeId == userId);

        if (protocol is null)
        {
            return Conflict(new
            {
                message = "Сначала сохраните черновик протокола."
            });
        }

        if (protocol.Status == JudgeProtocolStatus.Submitted)
        {
            return Conflict(new
            {
                message = "Протокол уже отправлен."
            });
        }

        var criteria = match.ProtocolTemplate?.Criteria.ToList();

        if (criteria is null || criteria.Count == 0)
        {
            return Conflict(new
            {
                message = "Критерии протокола отсутствуют."
            });
        }

        var scores = protocol.Scores
            .Select(x => new ScoreEntry(
                x.ProtocolCriterionId,
                x.Value))
            .ToList();

        var validationError = ProtocolScoreCalculator.Validate(
            criteria,
            scores,
            requireComplete: true);

        if (validationError is not null)
        {
            return Conflict(new
            {
                message = validationError
            });
        }

        protocol.Status = JudgeProtocolStatus.Submitted;
        protocol.SubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(ToResponse(protocol, criteria));
    }

    private Task<Match?> AssignedMatchAsync(
        int matchId,
        string userId)
    {
        return _db.Matches
            .AsNoTracking()
            .Include(x => x.Tournament)
            .Include(x => x.Discipline)
            .Include(x => x.ProtocolTemplate)
                .ThenInclude(x => x!.Criteria)
            .Include(x => x.MatchGroups)
                .ThenInclude(x => x.MatchTeams)
                    .ThenInclude(x => x.Team)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.Judges.Any(j => j.JudgeId == userId));
    }

    private static bool CanJudge(Match match)
    {
        return match.Status == MatchStatus.InProgress &&
               match.Tournament.Status == TournamentStatus.InProgress;
    }

    private static JudgeProtocolResponse ToResponse(
        JudgeProtocol protocol,
        IReadOnlyCollection<ProtocolCriterion> criteria)
    {
        var scores = protocol.Scores
            .Select(x => new ScoreEntry(
                x.ProtocolCriterionId,
                x.Value))
            .ToList();

        var complete = ProtocolScoreCalculator.Validate(
            criteria,
            scores,
            requireComplete: true) is null;

        var calculation = complete
            ? ProtocolScoreCalculator.Calculate(criteria, scores)
            : null;

        var order = criteria.ToDictionary(x => x.Id, x => x.Order);

        return new JudgeProtocolResponse
        {
            Id = protocol.Id,
            SideId = protocol.MatchGroupId,
            Status = protocol.Status,
            Comment = protocol.Comment,
            SubmittedAt = protocol.SubmittedAt,
            Total = calculation?.Total,

            Scores = scores
                .OrderBy(x => order.GetValueOrDefault(
                    x.CriterionId, int.MaxValue))
                .Select(x => new JudgeScoreResponse
                {
                    CriterionId = x.CriterionId,
                    Value = x.Value
                })
                .ToList()
        };
    }

    private static bool IsSqlConflict(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sql &&
               sql.Number is 2601 or 2627 or 1205;
    }
}
