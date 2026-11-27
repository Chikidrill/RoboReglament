
using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Matches;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;
using RoboReglament.Server.Services;
using System.Text;
namespace RoboReglament.Server.Controllers;

[ApiController]
[Authorize]
[Route(
    "api/tournaments/{tournamentId:int}/" +
    "disciplines/{disciplineId:int}/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public MatchesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Все выступления дисциплины.
    [HttpGet]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> GetAll(
        int tournamentId,
        int disciplineId)
    {
        if (await ManagedDisciplineAsync(
                tournamentId, disciplineId) is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        var matches = await DetailedMatches()
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId)
            .OrderBy(x => x.Number)
            .ToListAsync();

        return Ok(matches.Select(ToResponse));
    }

    // Команды, которые можно включать в выступления.
    [HttpGet("eligible-teams")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> GetEligibleTeams(
        int tournamentId,
        int disciplineId)
    {
        if (await ManagedDisciplineAsync(
                tournamentId, disciplineId) is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        var teams = await _db.TournamentApplications
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Status == ApplicationStatus.Approved &&
                x.Accreditation != null &&
                x.Accreditation.IsAccredited)
            .OrderBy(x => x.Team.Name)
            .Select(x => new EligibleMatchTeamResponse
            {
                Id = x.TeamId,
                Name = x.Team.Name
            })
            .ToListAsync();

        return Ok(teams);
    }

    // Просмотр конкретного выступления.
    // Доступен организатору или назначенному судье.
    [HttpGet("{matchId:int}")]
    [Authorize(Roles = UserRoles.Organizer + "," + UserRoles.Judge)]
    public async Task<IActionResult> GetById(
        int tournamentId,
        int disciplineId,
        int matchId)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var ownsTournament = User.IsInRole(UserRoles.Organizer) &&
            await _db.TournamentOrganizers.AnyAsync(x =>
                x.TournamentId == tournamentId &&
                x.UserId == userId);

        var assignedJudge = User.IsInRole(UserRoles.Judge) &&
            await _db.MatchJudges.AnyAsync(x =>
                x.MatchId == matchId &&
                x.JudgeId == userId &&
                x.Match.TournamentId == tournamentId &&
                x.Match.DisciplineId == disciplineId);

        if (!ownsTournament && !assignedJudge)
            return NotFound(new { message = "Матч недоступен." });

        var match = await DetailedMatches()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        return match is null
            ? NotFound(new { message = "Выступление не найдено." })
            : Ok(ToResponse(match));
    }

    // Будущий раздел "Мои выступления" в кабинете судьи.
    [HttpGet("~/api/judge/matches")]
    [Authorize(Roles = UserRoles.Judge)]
    public async Task<IActionResult> GetJudgeMatches()
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var matches = await DetailedMatches()
            .AsNoTracking()
            .Where(x => x.Judges.Any(j => j.JudgeId == userId))
            .OrderBy(x => x.ScheduledAt)
            .ThenBy(x => x.Number)
            .ToListAsync();

        return Ok(matches.Select(ToResponse));
    }

    // Ручное создание заезда или матча.
    [HttpPost]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Create(
        int tournamentId,
        int disciplineId,
        SaveMatchRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        var structureError = ValidateStructure(discipline, request);

        if (structureError is not null)
            return BadRequest(new { message = structureError });

        var teamError = await ValidateTeamsAsync(
            tournamentId, disciplineId, request.Sides);

        if (teamError is not null)
            return Conflict(new { message = teamError });

        var template = await ResolveTemplateAsync(
            tournamentId, disciplineId, request.ProtocolTemplateId);

        if (template is null)
        {
            return Conflict(new
            {
                message = "Выберите активный шаблон с критериями."
            });
        }

        var lastNumber = await _db.Matches
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId)
            .MaxAsync(x => (int?)x.Number) ?? 0;

        var match = new Match
        {
            TournamentId = tournamentId,
            DisciplineId = disciplineId,
            Number = lastNumber + 1,
            Name = Normalize(request.Name),
            ScheduledAt = NormalizeUtc(request.ScheduledAt),
            Status = MatchStatus.Scheduled,
            ProtocolTemplateId = template.Id,
            MatchGroups = BuildSides(request.Sides)
        };

        _db.Matches.Add(match);

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsSqlConflict(ex))
        {
            return Conflict(new
            {
                message = "Матчи изменились. Повторите операцию."
            });
        }

        var created = await FindDetailedAsync(
            tournamentId, disciplineId, match.Id);

        return StatusCode(201, ToResponse(created!));
    }

    // Редактирование до начала судейства.
    [HttpPut("{matchId:int}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Update(
        int tournamentId,
        int disciplineId,
        int matchId,
        SaveMatchRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        var match = await DetailedMatches()
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        if (match is null)
            return NotFound(new { message = "Выступление не найдено." });

        if (match.Status != MatchStatus.Scheduled ||
            await HasJudgeProtocolsAsync(matchId))
        {
            return Conflict(new
            {
                message = "Состав выступления уже зафиксирован."
            });
        }

        var structureError = ValidateStructure(discipline, request);

        if (structureError is not null)
            return BadRequest(new { message = structureError });

        var teamError = await ValidateTeamsAsync(
            tournamentId, disciplineId, request.Sides);

        if (teamError is not null)
            return Conflict(new { message = teamError });

        var template = await ResolveTemplateAsync(
            tournamentId,
            disciplineId,
            request.ProtocolTemplateId ?? match.ProtocolTemplateId,
            match.ProtocolTemplateId);

        if (template is null)
            return Conflict(new { message = "Шаблон протокола недоступен." });

        // Удаляем старый состав, затем создаём новый
        // внутри одной транзакции.
        foreach (var side in match.MatchGroups)
            _db.MatchTeams.RemoveRange(side.MatchTeams);

        _db.MatchGroups.RemoveRange(match.MatchGroups);

        await _db.SaveChangesAsync();

        match.Name = Normalize(request.Name);
        match.ScheduledAt = NormalizeUtc(request.ScheduledAt);
        match.ProtocolTemplateId = template.Id;

        var sides = BuildSides(request.Sides);

        foreach (var side in sides)
            side.MatchId = match.Id;

        _db.MatchGroups.AddRange(sides);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var updated = await FindDetailedAsync(
            tournamentId, disciplineId, matchId);

        return Ok(ToResponse(updated!));
    }

    // Удаление ещё не начатого выступления.
    [HttpDelete("{matchId:int}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Delete(
        int tournamentId,
        int disciplineId,
        int matchId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        var match = await DetailedMatches()
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        if (match is null)
            return NotFound(new { message = "Выступление не найдено." });

        if (match.Status != MatchStatus.Scheduled ||
            await HasJudgeProtocolsAsync(matchId))
        {
            return Conflict(new
            {
                message = "Удалить можно только выступление без судейских протоколов."
            });
        }

        _db.MatchJudges.RemoveRange(match.Judges);

        foreach (var side in match.MatchGroups)
            _db.MatchTeams.RemoveRange(side.MatchTeams);

        _db.MatchGroups.RemoveRange(match.MatchGroups);
        _db.Matches.Remove(match);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }


    [HttpPost("draw")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> Draw(
        int tournamentId,
        int disciplineId,
        DrawMatchesRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new
            {
                message = "Дисциплина недоступна."
            });

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        if (!request.TeamsPerSide.HasValue ||
            request.TeamsPerSide.Value <
                discipline.MinTeamsPerSide ||
            request.TeamsPerSide.Value >
                discipline.MaxTeamsPerSide)
        {
            return BadRequest(new
            {
                message =
                    "Количество команд в стороне " +
                    "не соответствует правилам дисциплины."
            });
        }

        var template = await ResolveTemplateAsync(
            tournamentId,
            disciplineId,
            request.ProtocolTemplateId);

        if (template is null)
        {
            return Conflict(new
            {
                message =
                    "Сначала создайте активный " +
                    "шаблон судейского протокола."
            });
        }

        var teamIds = await _db.TournamentApplications
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Status == ApplicationStatus.Approved &&
                x.Accreditation != null &&
                x.Accreditation.IsAccredited)
            .Select(x => x.TeamId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var existingMatches = await _db.Matches
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Status != MatchStatus.Cancelled)
            .Include(x => x.MatchGroups)
                .ThenInclude(x => x.MatchTeams)
            .AsSplitQuery()
            .ToListAsync();

        var existingFixtures = existingMatches
            .Select(match => match.MatchGroups
                .OrderBy(side => side.Number)
                .Select(side => side.MatchTeams
                    .Select(team => team.TeamId)
                    .ToArray())
                .ToArray())
            .ToList();

        List<int[][]> fixtures;

        try
        {
            fixtures = MatchDrawGenerator.Generate(
                discipline.Format,
                discipline.SidesPerEvent,
                request.TeamsPerSide.Value,
                teamIds,
                existingFixtures,
                request.Mode,
                request.AppearancesPerTeam);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        var lastNumber = await _db.Matches
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId)
            .MaxAsync(x => (int?)x.Number) ?? 0;

        var prefix = Normalize(request.NamePrefix)
            ?? (discipline.Format == CompetitionFormat.Run
                ? "Заезд"
                : "Матч");

        var createdMatches = new List<Match>();

        foreach (var fixture in fixtures)
        {
            var sides = fixture
                .Select((teamIdsInSide, index) =>
                    new SaveMatchSideRequest
                    {
                        Name =
                            discipline.ScoringMode ==
                            ScoringMode.PerAlliance
                                ? $"Союз №{index + 1}"
                                : $"Сторона №{index + 1}",

                        TeamIds = teamIdsInSide.ToList()
                    })
                .ToList();

            lastNumber++;

            createdMatches.Add(new Match
            {
                TournamentId = tournamentId,
                DisciplineId = disciplineId,
                Number = lastNumber,
                Name = $"{prefix} №{lastNumber}",
                Status = MatchStatus.Scheduled,
                ProtocolTemplateId = template.Id,
                MatchGroups = BuildSides(sides)
            });
        }

        _db.Matches.AddRange(createdMatches);

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsSqlConflict(ex))
        {
            return Conflict(new
            {
                message =
                    "Расписание уже изменилось. " +
                    "Обновите страницу и повторите действие."
            });
        }

        var createdIds = createdMatches
            .Select(x => x.Id)
            .ToArray();

        var created = await DetailedMatches()
            .AsNoTracking()
            .Where(x => createdIds.Contains(x.Id))
            .OrderBy(x => x.Number)
            .ToListAsync();

        return Ok(created.Select(ToResponse));
    }


    [HttpPost("draw/preview")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> PreviewDraw(
        int tournamentId,
        int disciplineId,
        DrawMatchesRequest request)
    {
        var discipline = await ManagedDisciplineAsync(
            tournamentId,
            disciplineId);

        if (discipline is null)
        {
            return NotFound(new
            {
                message = "Дисциплина недоступна."
            });
        }

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        if (!request.TeamsPerSide.HasValue ||
            request.TeamsPerSide.Value <
                discipline.MinTeamsPerSide ||
            request.TeamsPerSide.Value >
                discipline.MaxTeamsPerSide)
        {
            return BadRequest(new
            {
                message =
                    "Количество команд в стороне " +
                    "не соответствует правилам дисциплины."
            });
        }

        var template = await ResolveTemplateAsync(
            tournamentId,
            disciplineId,
            request.ProtocolTemplateId);

        if (template is null)
        {
            return Conflict(new
            {
                message =
                    "Сначала создайте активный " +
                    "шаблон судейского протокола."
            });
        }

        var teamIds = await _db.TournamentApplications
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Status == ApplicationStatus.Approved &&
                x.Accreditation != null &&
                x.Accreditation.IsAccredited)
            .Select(x => x.TeamId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var allMatches = await _db.Matches
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId)
            .Include(x => x.MatchGroups)
                .ThenInclude(x => x.MatchTeams)
            .AsSplitQuery()
            .ToListAsync();

        var existingFixtures = allMatches
            .Where(x => x.Status != MatchStatus.Cancelled)
            .Select(match => match.MatchGroups
                .OrderBy(side => side.Number)
                .Select(side => side.MatchTeams
                    .Select(team => team.TeamId)
                    .ToArray())
                .ToArray())
            .ToList();

        List<int[][]> fixtures;

        try
        {
            fixtures = MatchDrawGenerator.Generate(
                discipline.Format,
                discipline.SidesPerEvent,
                request.TeamsPerSide.Value,
                teamIds,
                existingFixtures,
                request.Mode,
                request.AppearancesPerTeam);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        for (var i = fixtures.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);

            (fixtures[i], fixtures[j]) =
                (fixtures[j], fixtures[i]);
        }

        var lastNumber = allMatches.Count == 0
            ? 0
            : allMatches.Max(x => x.Number);

        var prefix = Normalize(request.NamePrefix)
            ?? (discipline.Format == CompetitionFormat.Run
                ? "Заезд"
                : "Матч");

        var previewMatches = fixtures
            .Select((fixture, index) =>
                new DrawPreviewMatchResponse
                {
                    Number = lastNumber + index + 1,

                    Name =
                        $"{prefix} №{lastNumber + index + 1}",

                    Sides = fixture
                        .Select((ids, sideIndex) =>
                            new SaveMatchSideRequest
                            {
                                Name =
                                    discipline.ScoringMode ==
                                    ScoringMode.PerAlliance
                                        ? $"Союз №{sideIndex + 1}"
                                        : $"Сторона №{sideIndex + 1}",

                                TeamIds = ids.ToList()
                            })
                        .ToList()
                })
            .ToList();

        return Ok(new DrawPreviewResponse
        {
            Snapshot = ComputeDrawSnapshot(
                discipline,
                template.Id,
                teamIds,
                allMatches),

            ProtocolTemplateId = template.Id,
            Matches = previewMatches
        });
    }


    [HttpPost("draw/confirm")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> ConfirmDraw(
        int tournamentId,
        int disciplineId,
        ConfirmDrawRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new
            {
                message = "Дисциплина недоступна."
            });

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        var parameters = request.Parameters;

        if (parameters is null ||
            !parameters.TeamsPerSide.HasValue ||
            parameters.TeamsPerSide.Value <
                discipline.MinTeamsPerSide ||
            parameters.TeamsPerSide.Value >
                discipline.MaxTeamsPerSide ||
            parameters.AppearancesPerTeam is < 1 or > 30 ||
            !Enum.IsDefined(parameters.Mode))
        {
            return BadRequest(new
            {
                message = "Некорректные параметры жеребьёвки."
            });
        }

        if (request.Matches is null ||
            request.Matches.Count is < 1 or > 200)
        {
            return BadRequest(new
            {
                message = "Количество матчей должно быть от 1 до 200."
            });
        }

        var template = await ResolveTemplateAsync(
            tournamentId,
            disciplineId,
            parameters.ProtocolTemplateId);

        if (template is null)
        {
            return Conflict(new
            {
                message = "Шаблон протокола недоступен."
            });
        }

        var teamIds = await _db.TournamentApplications
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Status == ApplicationStatus.Approved &&
                x.Accreditation != null &&
                x.Accreditation.IsAccredited)
            .Select(x => x.TeamId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var allMatches = await _db.Matches
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId)
            .Include(x => x.MatchGroups)
                .ThenInclude(x => x.MatchTeams)
            .AsSplitQuery()
            .ToListAsync();

        var currentSnapshot = ComputeDrawSnapshot(
            discipline,
            template.Id,
            teamIds,
            allMatches);

        if (!string.Equals(
            currentSnapshot,
            request.Snapshot,
            StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new
            {
                message =
                    "Расписание или состав участников изменились. " +
                    "Проведите предварительную жеребьёвку заново."
            });
        }

        var eligible = teamIds.ToHashSet();

        var existingFixtures = allMatches
            .Where(x => x.Status != MatchStatus.Cancelled)
            .Select(match => match.MatchGroups
                .OrderBy(side => side.Number)
                .Select(side => side.MatchTeams
                    .Select(team => team.TeamId)
                    .ToArray())
                .ToArray())
            .ToList();

        // Проверяем структуру каждого подтверждаемого матча.
        foreach (var match in request.Matches)
        {
            if (match is null ||
                match.Sides is null ||
                match.Sides.Count != discipline.SidesPerEvent ||
                match.Name?.Length > 200)
            {
                return BadRequest(new
                {
                    message = "Некорректная структура матча."
                });
            }

            var ids = new List<int>();

            foreach (var side in match.Sides)
            {
                if (side is null ||
                    side.TeamIds is null ||
                    side.TeamIds.Count != parameters.TeamsPerSide.Value ||
                    side.Name?.Length > 200)
                {
                    return BadRequest(new
                    {
                        message =
                            "Количество команд в стороне " +
                            "не соответствует настройкам жеребьёвки."
                    });
                }

                ids.AddRange(side.TeamIds);
            }

            if (ids.Count != ids.Distinct().Count())
            {
                return BadRequest(new
                {
                    message =
                        "Одна команда не может занимать " +
                        "несколько мест в одном матче."
                });
            }

            if (ids.Any(id => !eligible.Contains(id)))
            {
                return Conflict(new
                {
                    message =
                        "В жеребьёвке есть команда без аккредитации."
                });
            }
        }

        // Проверяем, что ручные изменения не нарушили
        // выбранный способ жеребьёвки.
        if (parameters.Mode == MatchDrawMode.FixedAppearances)
        {
            var previousCounts = teamIds.ToDictionary(
                id => id, _ => 0);

            foreach (var fixture in existingFixtures)
            {
                foreach (var id in fixture.SelectMany(x => x))
                {
                    if (!previousCounts.ContainsKey(id))
                    {
                        return Conflict(new
                        {
                            message =
                                "Состав существующих матчей изменился."
                        });
                    }

                    previousCounts[id]++;
                }
            }

            if (previousCounts.Values.Distinct().Count() != 1)
            {
                return Conflict(new
                {
                    message =
                        "Существующие выступления распределены " +
                        "неравномерно."
                });
            }

            var expectedNewCount =
                parameters.AppearancesPerTeam -
                previousCounts.Values.First();

            if (expectedNewCount <= 0)
            {
                return Conflict(new
                {
                    message =
                        "Указанное количество выступлений " +
                        "уже достигнуто."
                });
            }

            var newCounts = teamIds.ToDictionary(
                id => id, _ => 0);

            foreach (var id in request.Matches
                .SelectMany(x => x.Sides)
                .SelectMany(x => x.TeamIds))
            {
                newCounts[id]++;
            }

            if (newCounts.Values.Any(
                count => count != expectedNewCount))
            {
                return BadRequest(new
                {
                    message =
                        "После изменений каждая команда должна " +
                        "иметь одинаковое заданное число выступлений."
                });
            }
        }
        else
        {
            // В режиме всех уникальных встреч проверяем
            // полный набор комбинаций.
            List<int[][]> expected;

            try
            {
                expected = MatchDrawGenerator.Generate(
                    discipline.Format,
                    discipline.SidesPerEvent,
                    parameters.TeamsPerSide.Value,
                    teamIds,
                    existingFixtures,
                    parameters.Mode,
                    parameters.AppearancesPerTeam);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }

            var expectedKeys = expected
                .Select(x => DrawFixtureKey(
                    x.Select(side => side.AsEnumerable())))
                .ToHashSet();

            var receivedKeys = request.Matches
                .Select(x => DrawFixtureKey(
                    x.Sides.Select(side => side.TeamIds.AsEnumerable())))
                .ToHashSet();

            if (expectedKeys.Count != request.Matches.Count ||
                !expectedKeys.SetEquals(receivedKeys))
            {
                return BadRequest(new
                {
                    message =
                        "Изменённый набор матчей нарушает " +
                        "режим всех уникальных встреч."
                });
            }
        }

        var lastNumber = allMatches.Count == 0
            ? 0
            : allMatches.Max(x => x.Number);

        var createdMatches = new List<Match>();

        foreach (var proposed in request.Matches)
        {
            lastNumber++;

            createdMatches.Add(new Match
            {
                TournamentId = tournamentId,
                DisciplineId = disciplineId,
                Number = lastNumber,
                Name = Normalize(proposed.Name),
                Status = MatchStatus.Scheduled,
                ProtocolTemplateId = template.Id,
                MatchGroups = BuildSides(proposed.Sides)
            });
        }

        _db.Matches.AddRange(createdMatches);

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsSqlConflict(ex))
        {
            return Conflict(new
            {
                message =
                    "Расписание изменилось. " +
                    "Сформируйте предварительный вариант заново."
            });
        }

        var createdIds = createdMatches
            .Select(x => x.Id)
            .ToArray();

        var created = await DetailedMatches()
            .AsNoTracking()
            .Where(x => createdIds.Contains(x.Id))
            .OrderBy(x => x.Number)
            .ToListAsync();

        return Ok(created.Select(ToResponse).ToList());
    }


    [HttpPost("{matchId:int}/judges")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> AssignJudge(
        int tournamentId,
        int disciplineId,
        int matchId,
        AssignJudgeRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        var match = await _db.Matches
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        if (match is null)
            return NotFound(new { message = "Выступление не найдено." });

        if (match.Status != MatchStatus.Scheduled ||
            await HasJudgeProtocolsAsync(matchId))
        {
            return Conflict(new
            {
                message = "Назначения судей уже зафиксированы."
            });
        }

        var judge = await _users.Users
            .FirstOrDefaultAsync(x => x.PublicId == request.PublicId);

        if (judge is null ||
            !await _users.IsInRoleAsync(judge, UserRoles.Judge))
        {
            return NotFound(new
            {
                message = "Пользователь с ролью Judge не найден."
            });
        }

        if (await _db.MatchJudges.AnyAsync(x =>
            x.MatchId == matchId &&
            x.JudgeId == judge.Id))
        {
            return Conflict(new
            {
                message = "Судья уже назначен."
            });
        }

        var assignment = new MatchJudge
        {
            MatchId = matchId,
            JudgeId = judge.Id,
            AssignedAt = DateTime.UtcNow
        };

        _db.MatchJudges.Add(assignment);

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsSqlConflict(ex))
        {
            return Conflict(new
            {
                message = "Назначение уже существует или данные изменились."
            });
        }

        return Ok(new MatchJudgeResponse
        {
            Id = judge.Id,
            PublicId = judge.PublicId,
            FirstName = judge.FirstName,
            LastName = judge.LastName,
            MiddleName = judge.MiddleName,
            AssignedAt = assignment.AssignedAt
        });
    }

    // Снятие судьи до начала выступления.
    [HttpDelete("{matchId:int}/judges/{publicId:long}")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> RemoveJudge(
        int tournamentId,
        int disciplineId,
        int matchId,
        long publicId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        if (!CanPrepare(discipline.Tournament.Status))
            return PreparationClosed();

        var match = await _db.Matches
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        if (match is null)
            return NotFound(new { message = "Выступление не найдено." });

        if (match.Status != MatchStatus.Scheduled ||
            await HasJudgeProtocolsAsync(matchId))
        {
            return Conflict(new
            {
                message = "Назначения судей уже зафиксированы."
            });
        }

        var assignment = await _db.MatchJudges
            .FirstOrDefaultAsync(x =>
                x.MatchId == matchId &&
                x.Judge.PublicId == publicId);

        if (assignment is null)
            return NotFound(new { message = "Судья не назначен." });

        _db.MatchJudges.Remove(assignment);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    // Начало, завершение или отмена выступления.
    [HttpPut("{matchId:int}/status")]
    [Authorize(Roles = UserRoles.Organizer)]
    public async Task<IActionResult> UpdateStatus(
        int tournamentId,
        int disciplineId,
        int matchId,
        UpdateMatchStatusRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var discipline = await ManagedDisciplineAsync(
            tournamentId, disciplineId);

        if (discipline is null)
            return NotFound(new { message = "Дисциплина недоступна." });

        var match = await DetailedMatches()
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        if (match is null)
            return NotFound(new { message = "Выступление не найдено." });

        if (match.Status == MatchStatus.Scheduled &&
            request.Status == MatchStatus.InProgress)
        {
            if (discipline.Tournament.Status != TournamentStatus.InProgress)
            {
                return Conflict(new
                {
                    message = "Сначала переведите турнир в статус InProgress."
                });
            }

            if (match.ProtocolTemplateId is null ||
                !await _db.ProtocolCriteria.AnyAsync(x =>
                    x.ProtocolTemplateId == match.ProtocolTemplateId))
            {
                return Conflict(new
                {
                    message = "Необходимо назначить шаблон с критериями."
                });
            }

            if (match.Judges.Count == 0 ||
                !ValidSavedSides(discipline, match))
            {
                return Conflict(new
                {
                    message = "Проверьте состав выступления и судей."
                });
            }

            match.Status = MatchStatus.InProgress;
        }

        else if (match.Status == MatchStatus.InProgress &&
                 request.Status == MatchStatus.Completed)
        {
            if (discipline.Tournament.Status != TournamentStatus.InProgress)
                return PreparationClosed();

            var sideIds = match.MatchGroups
                .Select(x => x.Id)
                .ToArray();

            var submittedProtocols = await _db.JudgeProtocols
                .AsNoTracking()
                .Include(x => x.Scores)
                .Where(x =>
                    sideIds.Contains(x.MatchGroupId) &&
                    x.Status == JudgeProtocolStatus.Submitted)
                .ToListAsync();

            try
            {
                // Проверяем полноту протоколов и рассчитываем
                // итог по выбранной системе дисциплины.
                _ = MatchResultsCalculator.Calculate(
                    match,
                    submittedProtocols);
            }
            catch (JudgeScoresDisagreeException ex)
            {
                return Conflict(new
                {
                    code = "judge_score_mismatch",
                    message = ex.Message,
                    sideId = ex.SideId,
                    criterionId = ex.CriterionId
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    code = "result_calculation_failed",
                    message = ex.Message
                });
            }

            match.Status = MatchStatus.Completed;
        }

        else if (match.Status == MatchStatus.Scheduled &&
                 request.Status == MatchStatus.Cancelled)
        {
            if (!CanPrepare(discipline.Tournament.Status) ||
                await HasJudgeProtocolsAsync(match.Id))
            {
                return Conflict(new
                {
                    message = "Отменить это выступление нельзя."
                });
            }

            match.Status = MatchStatus.Cancelled;
        }
        else
        {
            return Conflict(new
            {
                message = "Недопустимый переход статуса выступления."
            });
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var updated = await FindDetailedAsync(
            tournamentId, disciplineId, matchId);

        return Ok(ToResponse(updated!));
    }


    private static string ComputeDrawSnapshot(
        Discipline discipline,
        int templateId,
        IReadOnlyCollection<int> eligibleTeamIds,
        IReadOnlyCollection<Match> allMatches)
    {
        var builder = new StringBuilder();

        builder
            .Append("draw-v1|")
            .Append(discipline.TournamentId).Append('|')
            .Append(discipline.Id).Append('|')
            .Append(templateId).Append('|')
            .Append(discipline.Format).Append('|')
            .Append(discipline.ScoringMode).Append('|')
            .Append(discipline.SidesPerEvent).Append('|')
            .Append(discipline.MinTeamsPerSide).Append('|')
            .Append(discipline.MaxTeamsPerSide);

        foreach (var teamId in eligibleTeamIds.OrderBy(x => x))
        {
            builder.Append("|team:").Append(teamId);
        }

        foreach (var match in allMatches.OrderBy(x => x.Id))
        {
            builder
                .Append("|match:")
                .Append(match.Id).Append(':')
                .Append(match.Number).Append(':')
                .Append(match.Status);

            foreach (var side in match.MatchGroups
                .OrderBy(x => x.Number))
            {
                builder
                    .Append("|side:")
                    .Append(side.Number);

                foreach (var team in side.MatchTeams
                    .OrderBy(x => x.TeamId))
                {
                    builder.Append(':').Append(team.TeamId);
                }
            }
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());

        return Convert.ToHexString(SHA256.HashData(bytes));
    }


    private static string DrawFixtureKey(
        IEnumerable<IEnumerable<int>> sides)
    {
        return string.Join(
            "|",
            sides
                .Select(side => string.Join(
                    ",",
                    side.OrderBy(id => id)))
                .OrderBy(value => value, StringComparer.Ordinal));
    }



    private IQueryable<Match> DetailedMatches()
    {
        return _db.Matches
            .Include(x => x.Discipline)
            .Include(x => x.ProtocolTemplate).ThenInclude(x => x!.Criteria)
            .Include(x => x.MatchGroups)
                .ThenInclude(x => x.MatchTeams)
                    .ThenInclude(x => x.Team)
            .Include(x => x.Judges)
                .ThenInclude(x => x.Judge)
            .AsSplitQuery();
    }

    private Task<Match?> FindDetailedAsync(
        int tournamentId,
        int disciplineId,
        int matchId)
    {
        return DetailedMatches()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == matchId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);
    }

    private Task<Discipline?> ManagedDisciplineAsync(
        int tournamentId,
        int disciplineId)
    {
        var userId = _users.GetUserId(User);

        return _db.Disciplines
            .AsNoTracking()
            .Include(x => x.Tournament)
            .FirstOrDefaultAsync(x =>
                x.Id == disciplineId &&
                x.TournamentId == tournamentId &&
                x.Tournament.Organizers.Any(o =>
                    o.UserId == userId));
    }

    private Task<bool> HasJudgeProtocolsAsync(int matchId)
    {
        return _db.JudgeProtocols
            .AnyAsync(x => x.MatchGroup.MatchId == matchId);
    }

    private Task<ProtocolTemplate?> ResolveTemplateAsync(
        int tournamentId,
        int disciplineId,
        int? requestedId,
        int? currentId = null)
    {
        var query = _db.ProtocolTemplates
            .AsNoTracking()
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Criteria.Any());

        if (requestedId.HasValue)
        {
            return query.FirstOrDefaultAsync(x =>
                x.Id == requestedId.Value &&
                (x.IsActive || x.Id == currentId));
        }

        return query
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<string?> ValidateTeamsAsync(
        int tournamentId,
        int disciplineId,
        List<SaveMatchSideRequest> sides)
    {
        var ids = sides
            .SelectMany(x => x.TeamIds)
            .ToArray();

        if (ids.Length != ids.Distinct().Count())
            return "Команда повторяется в нескольких сторонах выступления.";

        var eligibleCount = await _db.TournamentApplications
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId &&
                x.Status == ApplicationStatus.Approved &&
                x.Accreditation != null &&
                x.Accreditation.IsAccredited &&
                ids.Contains(x.TeamId))
            .Select(x => x.TeamId)
            .Distinct()
            .CountAsync();

        return eligibleCount == ids.Length
            ? null
            : "Некоторые команды не аккредитованы на эту дисциплину.";
    }

    private static string? ValidateStructure(
        Discipline discipline,
        SaveMatchRequest request)
    {
        if (request.Name is not null &&
            request.Name.Trim().Length > 200)
            return "Название выступления слишком длинное.";

        if (request.ScheduledAt.HasValue &&
            request.ScheduledAt.Value.Kind == DateTimeKind.Unspecified)
        {
            return "Передайте дату в UTC с суффиксом Z.";
        }

        if (request.Sides is null ||
            request.Sides.Count != discipline.SidesPerEvent)
        {
            return $"Должно быть ровно {discipline.SidesPerEvent} сторон.";
        }

        foreach (var side in request.Sides)
        {
            if (side is null || side.TeamIds is null)
                return "Некорректный состав стороны.";

            if (side.Name is not null &&
                side.Name.Trim().Length > 200)
                return "Название стороны слишком длинное.";

            if (side.TeamIds.Count < discipline.MinTeamsPerSide ||
                side.TeamIds.Count > discipline.MaxTeamsPerSide ||
                side.TeamIds.Any(id => id <= 0))
            {
                return "Количество команд в стороне не соответствует правилам.";
            }
        }

        return null;
    }

    private static List<MatchGroup> BuildSides(
        List<SaveMatchSideRequest> sides)
    {
        return sides.Select((side, index) => new MatchGroup
        {
            Number = index + 1,
            Name = Normalize(side.Name),
            MatchTeams = side.TeamIds
                .Select((teamId, position) => new MatchTeam
                {
                    TeamId = teamId,
                    Position = position + 1
                })
                .ToList()
        }).ToList();
    }

    private static bool ValidSavedSides(
        Discipline discipline,
        Match match)
    {
        return match.MatchGroups.Count == discipline.SidesPerEvent &&
               match.MatchGroups.All(side =>
                   side.MatchTeams.Count >= discipline.MinTeamsPerSide &&
                   side.MatchTeams.Count <= discipline.MaxTeamsPerSide);
    }

   

    private static bool CanPrepare(TournamentStatus status)
    {
        return status is TournamentStatus.Registration
            or TournamentStatus.InProgress;
    }

    private IActionResult PreparationClosed()
    {
        return Conflict(new
        {
            message = "Изменять выступления на этом этапе турнира нельзя."
        });
    }

    private static string? Normalize(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? null
            : text.Trim();
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        return value?.ToUniversalTime();
    }

    private static bool IsSqlConflict(DbUpdateException error)
    {
        return error.InnerException is SqlException sql &&
               sql.Number is 2601 or 2627 or 1205;
    }

    private static MatchResponse ToResponse(Match match)
    {
        return new MatchResponse
        {
            Id = match.Id,
            TournamentId = match.TournamentId,
            DisciplineId = match.DisciplineId,
            Format = match.Discipline.Format,
            ScoringMode = match.Discipline.ScoringMode,
            Number = match.Number,
            Name = match.Name,
            ScheduledAt = match.ScheduledAt,
            Status = match.Status,
            ProtocolTemplateId = match.ProtocolTemplateId,
            ProtocolTemplateName = match.ProtocolTemplate?.Name,

            Sides = match.MatchGroups
                .OrderBy(x => x.Number)
                .Select(side => new MatchSideResponse
                {
                    Id = side.Id,
                    Number = side.Number,
                    Name = side.Name,
                    Teams = side.MatchTeams
                        .OrderBy(x => x.Position)
                        .Select(x => new MatchTeamResponse
                        {
                            Id = x.Id,
                            TeamId = x.TeamId,
                            TeamName = x.Team.Name,
                            Position = x.Position
                        })
                        .ToList()
                })
                .ToList(),

            Judges = match.Judges
                .OrderBy(x => x.Judge.LastName)
                .ThenBy(x => x.Judge.FirstName)
                .Select(x => new MatchJudgeResponse
                {
                    Id = x.JudgeId,
                    PublicId = x.Judge.PublicId,
                    FirstName = x.Judge.FirstName,
                    LastName = x.Judge.LastName,
                    MiddleName = x.Judge.MiddleName,
                    AssignedAt = x.AssignedAt
                })
                .ToList()
        };
    }
}
