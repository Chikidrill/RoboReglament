
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Protocols;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Organizer)]
[Route(
    "api/tournaments/{tournamentId:int}/disciplines/" +
    "{disciplineId:int}/protocol-templates"
)]
public sealed class ProtocolTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProtocolTemplatesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Получение списка шаблонов дисциплины.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        int tournamentId,
        int disciplineId)
    {
        var error = await CheckAccessAsync(
            tournamentId, disciplineId);

        if (error is not null)
            return error;

        var templates = await _context.ProtocolTemplates
            .AsNoTracking()
            .Include(x => x.Criteria)
            .Where(x =>
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        var result = new List<ProtocolTemplateResponse>();

        foreach (var template in templates)
        {
            var locked = await IsLockedAsync(template.Id);
            result.Add(ToResponse(template, locked));
        }

        return Ok(result);
    }

    // Получение одного шаблона.
    [HttpGet("{templateId:int}")]
    public async Task<IActionResult> GetById(
        int tournamentId,
        int disciplineId,
        int templateId)
    {
        var error = await CheckAccessAsync(
            tournamentId, disciplineId);

        if (error is not null)
            return error;

        var template = await FindTemplateAsync(
            tournamentId, disciplineId, templateId);

        if (template is null)
            return TemplateNotFound();

        return Ok(ToResponse(
            template,
            await IsLockedAsync(templateId)));
    }

    // Создание нового шаблона вместе с критериями.
    [HttpPost]
    public async Task<IActionResult> Create(
        int tournamentId,
        int disciplineId,
        SaveProtocolTemplateRequest request)
    {
        var error = await CheckAccessAsync(
            tournamentId, disciplineId, write: true);

        if (error is not null)
            return error;

        var validation = ValidateRequest(request);

        if (validation is not null)
            return BadRequest(new { message = validation });

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        // В одной дисциплине только один активный шаблон.
        if (request.IsActive)
        {
            await _context.ProtocolTemplates
                .Where(x =>
                    x.TournamentId == tournamentId &&
                    x.DisciplineId == disciplineId &&
                    x.IsActive)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        x => x.IsActive, false));
        }

        var template = new ProtocolTemplate
        {
            TournamentId = tournamentId,
            DisciplineId = disciplineId,
            Name = request.Name.Trim(),
            Description = Normalize(request.Description),
            IsActive = request.IsActive,
            Criteria = BuildCriteria(request.Criteria)
        };

        _context.ProtocolTemplates.Add(template);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var response = ToResponse(template, false);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                tournamentId,
                disciplineId,
                templateId = template.Id
            },
            response);
    }

    // Обновление шаблона и списка критериев.
    // Список критериев заменяется целиком, атомарно.
    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> Update(
        int tournamentId,
        int disciplineId,
        int templateId,
        SaveProtocolTemplateRequest request)
    {
        var error = await CheckAccessAsync(
            tournamentId, disciplineId, write: true);

        if (error is not null)
            return error;

        var validation = ValidateRequest(request);

        if (validation is not null)
            return BadRequest(new { message = validation });

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        // Загружаем без Criteria, поскольку критерии будем
        // заменять отдельными запросами.
        var template = await _context.ProtocolTemplates
            .FirstOrDefaultAsync(x =>
                x.Id == templateId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        if (template is null)
            return TemplateNotFound();

        if (await IsLockedAsync(templateId))
            return LockedConflict();

        if (request.IsActive)
        {
            await _context.ProtocolTemplates
                .Where(x =>
                    x.TournamentId == tournamentId &&
                    x.DisciplineId == disciplineId &&
                    x.Id != templateId &&
                    x.IsActive)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        x => x.IsActive, false));
        }

        template.Name = request.Name.Trim();
        template.Description = Normalize(request.Description);
        template.IsActive = request.IsActive;

        // Удаляем старые критерии внутри транзакции.
        // При наличии судейских оценок выполнение
        // блокируется методом IsLockedAsync.
        await _context.ProtocolCriteria
            .Where(x => x.ProtocolTemplateId == templateId)
            .ExecuteDeleteAsync();

        var criteria = BuildCriteria(request.Criteria);

        foreach (var criterion in criteria)
            criterion.ProtocolTemplateId = templateId;

        _context.ProtocolCriteria.AddRange(criteria);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var updated = await FindTemplateAsync(
            tournamentId, disciplineId, templateId);

        return Ok(ToResponse(updated!, false));
    }

    // Удаление неиспользуемого шаблона.
    [HttpDelete("{templateId:int}")]
    public async Task<IActionResult> Delete(
        int tournamentId,
        int disciplineId,
        int templateId)
    {
        var error = await CheckAccessAsync(
            tournamentId, disciplineId, write: true);

        if (error is not null)
            return error;

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var template = await _context.ProtocolTemplates
            .FirstOrDefaultAsync(x =>
                x.Id == templateId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);

        if (template is null)
            return TemplateNotFound();

        // Удалять шаблон, связанный с матчами, нельзя.
        var hasMatches = await _context.Matches
            .AnyAsync(x => x.ProtocolTemplateId == templateId);

        if (hasMatches || await IsLockedAsync(templateId))
        {
            return Conflict(new
            {
                message =
                    "Нельзя удалить используемый шаблон протокола."
            });
        }

        await _context.ProtocolCriteria
            .Where(x => x.ProtocolTemplateId == templateId)
            .ExecuteDeleteAsync();

        _context.ProtocolTemplates.Remove(template);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    // Пробный расчёт результатов без сохранения оценок.
    [HttpPost("{templateId:int}/preview")]
    public async Task<IActionResult> Preview(
        int tournamentId,
        int disciplineId,
        int templateId,
        PreviewProtocolRequest request)
    {
        var error = await CheckAccessAsync(
            tournamentId, disciplineId);

        if (error is not null)
            return error;

        var template = await FindTemplateAsync(
            tournamentId, disciplineId, templateId);

        if (template is null)
            return TemplateNotFound();

        var criteria = template.Criteria
            .OrderBy(x => x.Order)
            .ToList();

        if (criteria.Count == 0)
        {
            return Conflict(new
            {
                message = "В протоколе отсутствуют критерии."
            });
        }

        if (request.Scores is null ||
            request.Scores.Count != criteria.Count ||
            request.Scores.Select(x => x.CriterionId)
                .Distinct().Count() != criteria.Count)
        {
            return BadRequest(new
            {
                message =
                    "Укажите по одной оценке для каждого критерия."
            });
        }

        var submitted = request.Scores
            .ToDictionary(x => x.CriterionId, x => x.Value);

        var result = new PreviewProtocolResponse
        {
            TemplateId = templateId
        };

        decimal maximum = 0;
        decimal total = 0;

        foreach (var criterion in criteria)
        {
            if (!submitted.TryGetValue(
                    criterion.Id, out var value))
            {
                return BadRequest(new
                {
                    message = "Передан неизвестный критерий."
                });
            }

            if (decimal.Round(value, 2) != value ||
                value < criterion.MinValue ||
                !criterion.MaxValue.HasValue ||
                value > criterion.MaxValue.Value)
            {
                return BadRequest(new
                {
                    message =
                        $"Недопустимый балл по критерию «{criterion.Name}»."
                });
            }

            var sign = criterion.IsPenalty ? -1m : 1m;

            var contribution = decimal.Round(
                sign * value * criterion.Weight,
                2,
                MidpointRounding.AwayFromZero);

            var bestValue = criterion.IsPenalty
                ? criterion.MinValue
                : criterion.MaxValue.Value;

            var bestContribution = decimal.Round(
                sign * bestValue * criterion.Weight,
                2,
                MidpointRounding.AwayFromZero);

            total += contribution;
            maximum += bestContribution;

            result.Criteria.Add(new PreviewCriterionResponse
            {
                CriterionId = criterion.Id,
                Name = criterion.Name,
                Value = value,
                Weight = criterion.Weight,
                IsPenalty = criterion.IsPenalty,
                Contribution = contribution
            });
        }

        result.Total = total;
        result.MaximumPossible = maximum;

        return Ok(result);
    }

    // Проверка доступа организатора к дисциплине.
    private async Task<IActionResult?> CheckAccessAsync(
        int tournamentId,
        int disciplineId,
        bool write = false)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
            return Unauthorized();

        var discipline = await _context.Disciplines
            .AsNoTracking()
            .Where(x =>
                x.Id == disciplineId &&
                x.TournamentId == tournamentId &&
                x.Tournament.Organizers.Any(
                    organizer => organizer.UserId == userId))
            .Select(x => new
            {
                x.Tournament.Status
            })
            .FirstOrDefaultAsync();

        if (discipline is null)
        {
            return NotFound(new
            {
                message = "Дисциплина не найдена или недоступна."
            });
        }

        if (write &&
            discipline.Status == TournamentStatus.Completed)
        {
            return Conflict(new
            {
                message = "Турнир завершён. Изменение запрещено."
            });
        }

        return null;
    }

    private Task<ProtocolTemplate?> FindTemplateAsync(
        int tournamentId,
        int disciplineId,
        int templateId)
    {
        return _context.ProtocolTemplates
            .AsNoTracking()
            .Include(x => x.Criteria)
            .FirstOrDefaultAsync(x =>
                x.Id == templateId &&
                x.TournamentId == tournamentId &&
                x.DisciplineId == disciplineId);
    }

    // Блокировка после появления оценок либо
    // отправленных судейских протоколов.
    private async Task<bool> IsLockedAsync(int templateId)
    {
        var hasScores = await _context.JudgeScores
            .AnyAsync(x =>
                x.ProtocolCriterion.ProtocolTemplateId == templateId);

        if (hasScores)
            return true;

        return await _context.JudgeProtocols
            .AnyAsync(x =>
                x.MatchGroup.Match.ProtocolTemplateId == templateId &&
                x.Status == JudgeProtocolStatus.Submitted);
    }

    private static List<ProtocolCriterion> BuildCriteria(
        List<SaveProtocolCriterionRequest> requests)
    {
        return requests.Select((x, index) =>
            new ProtocolCriterion
            {
                Name = x.Name.Trim(),
                Description = Normalize(x.Description),
                MinValue = x.MinValue,
                MaxValue = x.MaxValue,
                Weight = x.Weight,
                IsPenalty = x.IsPenalty,
                Order = index + 1
            }).ToList();
    }

    private static string? ValidateRequest(
        SaveProtocolTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Введите название протокола.";

        if (request.Name.Trim().Length > 200)
            return "Название протокола слишком длинное.";

        if (request.Description?.Trim().Length > 2000)
            return "Описание протокола слишком длинное.";

        if (request.Criteria is null ||
            request.Criteria.Count is < 1 or > 100)
        {
            return "Укажите от 1 до 100 критериев.";
        }

        for (var i = 0; i < request.Criteria.Count; i++)
        {
            var criterion = request.Criteria[i];

            if (criterion is null ||
                string.IsNullOrWhiteSpace(criterion.Name))
            {
                return $"Укажите название критерия №{i + 1}.";
            }

            if (criterion.Name.Trim().Length > 200 ||
                criterion.Description?.Trim().Length > 1000)
            {
                return $"Слишком длинное описание критерия №{i + 1}.";
            }

            if (criterion.MinValue < 0 ||
                criterion.MaxValue < criterion.MinValue ||
                criterion.MaxValue > 100000)
            {
                return $"Проверьте границы критерия №{i + 1}.";
            }

            if (criterion.Weight <= 0 ||
                criterion.Weight > 100)
            {
                return $"Недопустимый коэффициент критерия №{i + 1}.";
            }

            if (
                decimal.Round(criterion.MinValue, 2) !=
                criterion.MinValue ||
                decimal.Round(criterion.MaxValue, 2) !=
                criterion.MaxValue ||
                decimal.Round(criterion.Weight, 2) !=
                criterion.Weight
            )
            {
                return $"Допустимо не более двух знаков после запятой. " +
                       $"Критерий №{i + 1}.";
            }
        }

        var names = request.Criteria
            .Select(x => x.Name.Trim())
            .ToList();

        if (names.Distinct(
                StringComparer.OrdinalIgnoreCase).Count() !=
            names.Count)
        {
            return "Названия критериев внутри протокола должны различаться.";
        }

        return null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static ProtocolTemplateResponse ToResponse(
        ProtocolTemplate template,
        bool locked)
    {
        return new ProtocolTemplateResponse
        {
            Id = template.Id,
            TournamentId = template.TournamentId,
            DisciplineId = template.DisciplineId,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
            IsLocked = locked,
            CreatedAt = template.CreatedAt,

            Criteria = template.Criteria
                .OrderBy(x => x.Order)
                .Select(x => new ProtocolCriterionResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    MinValue = x.MinValue,
                    MaxValue = x.MaxValue,
                    Weight = x.Weight,
                    IsPenalty = x.IsPenalty,
                    Order = x.Order
                })
                .ToList()
        };
    }

    private IActionResult TemplateNotFound()
    {
        return NotFound(new
        {
            message = "Шаблон протокола не найден."
        });
    }

    private IActionResult LockedConflict()
    {
        return Conflict(new
        {
            message =
                "Изменять протокол с сохранёнными " +
                "судейскими оценками нельзя."
        });
    }
}
