
using RoboReglament.Server.Models;

namespace RoboReglament.Server.Services;

public sealed class SideResultCalculation
{
    public int SideId { get; init; }
    public int Number { get; init; }
    public string? Name { get; init; }

    public decimal TotalScore { get; init; }
    public int JudgeCount { get; init; }

    public bool IsWinner { get; set; }

    public List<int> TeamIds { get; init; } = new();
}

public sealed class MatchResultCalculation
{
    public int MatchId { get; init; }

    public JudgeAggregationMethod AggregationMethod { get; init; }

    public bool IsDraw { get; set; }

    public List<SideResultCalculation> Sides { get; init; }
        = new();
}

public sealed class JudgeScoresDisagreeException
    : InvalidOperationException
{
    public int SideId { get; }
    public int CriterionId { get; }

    public JudgeScoresDisagreeException(
        int sideId,
        int criterionId,
        string criterionName)
        : base(
            $"Судьи указали разные фактические баллы " +
            $"по критерию «{criterionName}»."
        )
    {
        SideId = sideId;
        CriterionId = criterionId;
    }
}

public static class MatchResultsCalculator
{
    private sealed record JudgeCalculation(
        string JudgeId,
        decimal Total,
        Dictionary<int, decimal> Values
    );

    public static MatchResultCalculation Calculate(
        Match match,
        IReadOnlyCollection<JudgeProtocol> protocols)
    {
        if (match.Status != MatchStatus.InProgress &&
            match.Status != MatchStatus.Completed)
        {
            throw new InvalidOperationException(
                "Выступление ещё не началось или отменено.");
        }

        var criteria = match.ProtocolTemplate?.Criteria
            .OrderBy(x => x.Order)
            .ToList();

        if (criteria is null || criteria.Count == 0)
        {
            throw new InvalidOperationException(
                "У выступления нет шаблона с критериями.");
        }

        var judgeIds = match.Judges
            .Select(x => x.JudgeId)
            .ToHashSet(StringComparer.Ordinal);

        if (judgeIds.Count == 0)
        {
            throw new InvalidOperationException(
                "На выступление не назначены судьи.");
        }

        if (match.MatchGroups.Count == 0)
        {
            throw new InvalidOperationException(
                "У выступления отсутствуют стороны.");
        }

        var method = match.Discipline.JudgeAggregationMethod;

        if (!Enum.IsDefined(method))
        {
            throw new InvalidOperationException(
                "Неизвестный способ подсчёта результатов.");
        }

        var result = new MatchResultCalculation
        {
            MatchId = match.Id,
            AggregationMethod = method
        };

        foreach (var side in match.MatchGroups.OrderBy(x => x.Number))
        {
            var sideProtocols = protocols
                .Where(x =>
                    x.MatchGroupId == side.Id &&
                    x.Status == JudgeProtocolStatus.Submitted &&
                    x.JudgeId is not null &&
                    judgeIds.Contains(x.JudgeId))
                .ToList();

            if (sideProtocols.Count != judgeIds.Count ||
                sideProtocols
                    .Select(x => x.JudgeId)
                    .Distinct()
                    .Count() != judgeIds.Count)
            {
                throw new InvalidOperationException(
                    $"Не все судьи отправили протоколы " +
                    $"для стороны №{side.Number}.");
            }

            var judgeResults = new List<JudgeCalculation>();

            foreach (var protocol in sideProtocols)
            {
                var scores = protocol.Scores
                    .Select(x => new ScoreEntry(
                        x.ProtocolCriterionId,
                        x.Value))
                    .ToList();

                var validationError =
                    ProtocolScoreCalculator.Validate(
                        criteria,
                        scores,
                        requireComplete: true);

                if (validationError is not null)
                {
                    throw new InvalidOperationException(
                        $"Протокол №{protocol.Id}: " +
                        validationError);
                }

                var calculation =
                    ProtocolScoreCalculator.Calculate(
                        criteria,
                        scores);

                judgeResults.Add(new JudgeCalculation(
                    protocol.JudgeId!,
                    calculation.Total,
                    scores.ToDictionary(
                        x => x.CriterionId,
                        x => x.Value)
                ));
            }

            decimal total;

            switch (method)
            {
                case JudgeAggregationMethod.Average:
                    total = decimal.Round(
                        judgeResults.Average(x => x.Total),
                        2,
                        MidpointRounding.AwayFromZero);
                    break;

                case JudgeAggregationMethod.Sum:
                    total = judgeResults.Sum(x => x.Total);
                    break;

                case JudgeAggregationMethod.AgreedActual:
                    // Фактические баллы сверяем отдельно
                    // по каждому критерию.
                    foreach (var criterion in criteria)
                    {
                        var differentValues = judgeResults
                            .Select(x =>
                                x.Values[criterion.Id])
                            .Distinct()
                            .Count();

                        if (differentValues > 1)
                        {
                            throw new JudgeScoresDisagreeException(
                                side.Id,
                                criterion.Id,
                                criterion.Name);
                        }
                    }

                    // Баллы подтверждены всеми судьями.
                    // Берём один результат, а не их сумму.
                    total = judgeResults[0].Total;
                    break;

                default:
                    throw new InvalidOperationException(
                        "Способ подсчёта не поддерживается.");
            }

            result.Sides.Add(new SideResultCalculation
            {
                SideId = side.Id,
                Number = side.Number,
                Name = side.Name,
                TotalScore = total,
                JudgeCount = judgeResults.Count,

                TeamIds = side.MatchTeams
                    .Select(x => x.TeamId)
                    .OrderBy(x => x)
                    .ToList()
            });
        }

        // В одиночном заезде определение победителя
        // происходит по итогам дисциплины, а не заезда.
        if (match.Discipline.Format == CompetitionFormat.Match &&
            result.Sides.Count >= 2)
        {
            var highestScore = result.Sides
                .Max(x => x.TotalScore);

            var leaders = result.Sides
                .Where(x => x.TotalScore == highestScore)
                .ToList();

            result.IsDraw = leaders.Count > 1;

            if (!result.IsDraw)
            {
                leaders[0].IsWinner = true;
            }
        }

        return result;
    }
}
