
using RoboReglament.Server.Models;

namespace RoboReglament.Server.Services;

public sealed record ScoreEntry(
    int CriterionId,
    decimal Value
);

public sealed record ScoreContribution(
    int CriterionId,
    string Name,
    decimal Value,
    decimal Weight,
    bool IsPenalty,
    decimal Contribution
);

public sealed class ScoreCalculation
{
    public decimal Total { get; init; }
    public decimal MaximumPossible { get; init; }

    public List<ScoreContribution> Criteria { get; init; } = new();
}

public static class ProtocolScoreCalculator
{
    public static string? Validate(
        IReadOnlyCollection<ProtocolCriterion> criteria,
        IReadOnlyCollection<ScoreEntry> scores,
        bool requireComplete)
    {
        if (criteria.Count == 0)
            return "В шаблоне отсутствуют критерии.";

        if (scores.Select(x => x.CriterionId).Distinct().Count()
            != scores.Count)
        {
            return "Один критерий указан несколько раз.";
        }

        if (requireComplete && scores.Count != criteria.Count)
        {
            return "Заполните все критерии протокола.";
        }

        var allowed = criteria.ToDictionary(x => x.Id);

        foreach (var score in scores)
        {
            if (!allowed.TryGetValue(
                    score.CriterionId, out var criterion))
            {
                return "Передан неизвестный критерий.";
            }

            if (!criterion.MaxValue.HasValue ||
                score.Value < criterion.MinValue ||
                score.Value > criterion.MaxValue.Value ||
                decimal.Round(score.Value, 2) != score.Value)
            {
                return $"Недопустимый балл по критерию «{criterion.Name}».";
            }
        }

        return null;
    }

    public static ScoreCalculation Calculate(
        IReadOnlyCollection<ProtocolCriterion> criteria,
        IReadOnlyCollection<ScoreEntry> scores)
    {
        // Вызывать только после успешной Validate().
        var values = scores.ToDictionary(
            x => x.CriterionId,
            x => x.Value);

        var total = 0m;
        var maximum = 0m;

        var contributions = new List<ScoreContribution>();

        foreach (var criterion in criteria.OrderBy(x => x.Order))
        {
            var sign = criterion.IsPenalty ? -1m : 1m;

            var bestValue = criterion.IsPenalty
                ? criterion.MinValue
                : criterion.MaxValue!.Value;

            var bestContribution = decimal.Round(
                sign * bestValue * criterion.Weight,
                2,
                MidpointRounding.AwayFromZero);

            maximum += bestContribution;

            if (!values.TryGetValue(criterion.Id, out var value))
                continue;

            var contribution = decimal.Round(
                sign * value * criterion.Weight,
                2,
                MidpointRounding.AwayFromZero);

            total += contribution;

            contributions.Add(new ScoreContribution(
                criterion.Id,
                criterion.Name,
                value,
                criterion.Weight,
                criterion.IsPenalty,
                contribution
            ));
        }

        return new ScoreCalculation
        {
            Total = total,
            MaximumPossible = maximum,
            Criteria = contributions
        };
    }
}
