
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Results;
using RoboReglament.Server.Models;
using RoboReglament.Server.Services;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId:int}/results")]
public sealed class TournamentResultsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TournamentResultsController> _logger;

    public TournamentResultsController(
        ApplicationDbContext db,
        ILogger<TournamentResultsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetResults(
        int tournamentId,
        CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .AsNoTracking()
            .Include(x => x.Disciplines)
            .FirstOrDefaultAsync(
                x => x.Id == tournamentId,
                cancellationToken);

        // Черновики турниров публично не раскрываем.
        if (tournament is null ||
            tournament.Status == TournamentStatus.Draft)
        {
            return NotFound(new
            {
                message = "Турнир не найден."
            });
        }

        var response = new TournamentResultsResponse
        {
            TournamentId = tournament.Id,
            TournamentName = tournament.Name,
            Status = tournament.Status,

            Disciplines = tournament.Disciplines
                .OrderBy(x => x.Name)
                .Select(x => new DisciplineResultsResponse
                {
                    DisciplineId = x.Id,
                    Name = x.Name,
                    Format = x.Format,
                    ScoringMode = x.ScoringMode,
                    AggregationMethod = x.JudgeAggregationMethod,
                    RankingMethod = x.RankingMethod,
                    WinPoints = x.WinPoints,
                    DrawPoints = x.DrawPoints,
                    LossPoints = x.LossPoints,
                })
                .ToList()
        };

        // До начала турнира возвращаем только дисциплины.
        if (tournament.Status == TournamentStatus.Registration)
        {
            return Ok(response);
        }

        // Получаем исключительно завершённые выступления.
        var matches = await _db.Matches
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.Status == MatchStatus.Completed)
            .Include(x => x.Discipline)
            .Include(x => x.ProtocolTemplate)
                .ThenInclude(x => x!.Criteria)
            .Include(x => x.Judges)
            .Include(x => x.MatchGroups)
                .ThenInclude(x => x.MatchTeams)
                    .ThenInclude(x => x.Team)
            .AsSplitQuery()
            .OrderBy(x => x.DisciplineId)
            .ThenBy(x => x.Number)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            return Ok(response);
        }

        // Собираем ID сторон всех завершённых выступлений.
        var sideIds = matches
            .SelectMany(x => x.MatchGroups)
            .Select(x => x.Id)
            .ToArray();

        // Загружаем только отправленные протоколы.
        var protocols = await _db.JudgeProtocols
            .AsNoTracking()
            .Include(x => x.Scores)
            .Where(x =>
                sideIds.Contains(x.MatchGroupId) &&
                x.Status == JudgeProtocolStatus.Submitted)
            .ToListAsync(cancellationToken);

        var disciplinesById = response.Disciplines
            .ToDictionary(x => x.DisciplineId);

        foreach (var match in matches)
        {
            var matchSideIds = match.MatchGroups
                .Select(x => x.Id)
                .ToHashSet();

            var matchProtocols = protocols
                .Where(x =>
                    matchSideIds.Contains(x.MatchGroupId))
                .ToList();

            MatchResultCalculation calculation;

            try
            {
                calculation = MatchResultsCalculator.Calculate(
                    match,
                    matchProtocols);
            }
            catch (InvalidOperationException ex)
            {
                // Некорректное завершённое выступление
                // требует исправления данных на сервере.
                _logger.LogError(
                    ex,
                    "Failed to calculate match {MatchId}",
                    match.Id);

                return Problem(
                    title: "Не удалось рассчитать результаты турнира.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var result = new MatchResultResponse
            {
                MatchId = match.Id,
                Number = match.Number,
                Name = match.Name,
                ScheduledAt = match.ScheduledAt,
                IsDraw = calculation.IsDraw
            };

            foreach (var sideResult in calculation.Sides)
            {
                var side = match.MatchGroups.Single(
                    x => x.Id == sideResult.SideId);

                result.Sides.Add(new SideResultResponse
                {
                    SideId = sideResult.SideId,
                    Number = sideResult.Number,
                    Name = sideResult.Name,

                    TotalScore = sideResult.TotalScore,
                    JudgeCount = sideResult.JudgeCount,
                    IsWinner = sideResult.IsWinner,

                    Teams = side.MatchTeams
                        .OrderBy(x =>
                            x.Position ?? int.MaxValue)
                        .ThenBy(x => x.Team.Name)
                        .Select(x => new ResultTeamResponse
                        {
                            TeamId = x.TeamId,
                            Name = x.Team.Name
                        })
                        .ToList()
                });
            }

            disciplinesById[match.DisciplineId]
                .Matches.Add(result);
        }


        // Формируем рейтинговые таблицы всех дисциплин.
        foreach (var discipline in response.Disciplines)
        {
            try
            {

                discipline.Standings =
                    DisciplineRankingCalculator.Calculate(
                        discipline.Format,
                        discipline.RankingMethod,
                        discipline.Matches,
                        discipline.WinPoints,
                        discipline.DrawPoints,
                        discipline.LossPoints);

            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to calculate ranking for discipline {DisciplineId}",
                    discipline.DisciplineId);

                return Problem(
                    title: "Не удалось сформировать рейтинг дисциплины.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        return Ok(response);
    }
}
