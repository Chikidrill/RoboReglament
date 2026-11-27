
using RoboReglament.Server.DTOs.Results;
using RoboReglament.Server.Models;

namespace RoboReglament.Server.Services;

public static class DisciplineRankingCalculator
{
    private sealed class TeamStatistics
    {
        public int TeamId { get; init; }

        public string TeamName { get; init; }
            = string.Empty;

        public List<decimal> Scores { get; } = new();

        public int Wins { get; set; }

        public int Draws { get; set; }

        public int Losses { get; set; }

        public int TournamentPoints { get; set; }
    }

    public static List<TeamRankingResponse> Calculate(
        CompetitionFormat format,
        RankingMethod rankingMethod,
        IReadOnlyCollection<MatchResultResponse> matches, int winPoints,int drawPoints,int lossPoints)
    {
        ValidateRankingMethod(format, rankingMethod);

        var teams = new Dictionary<int, TeamStatistics>();

        foreach (var match in matches)
        {
            ValidateMatch(format, match);

            // Команда не должна встречаться в нескольких
            // сторонах одного выступления.
            var usedTeamIds = new HashSet<int>();

            var maximumScore = format == CompetitionFormat.Match
                ? match.Sides.Max(x => x.TotalScore)
                : 0m;

            var leaderCount = format == CompetitionFormat.Match
                ? match.Sides.Count(
                    x => x.TotalScore == maximumScore)
                : 0;

            foreach (var side in match.Sides)
            {
                if (side.Teams.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Сторона {side.SideId} не содержит команд.");
                }

                foreach (var team in side.Teams)
                {
                    if (!usedTeamIds.Add(team.TeamId))
                    {
                        throw new InvalidOperationException(
                            $"Команда {team.TeamId} повторяется " +
                            $"в выступлении {match.MatchId}.");
                    }

                    if (!teams.TryGetValue(
                        team.TeamId, out var statistics))
                    {
                        statistics = new TeamStatistics
                        {
                            TeamId = team.TeamId,
                            TeamName = team.Name
                        };

                        teams.Add(team.TeamId, statistics);
                    }

                    // В союзе каждая команда получает
                    // результат своей стороны целиком.
                    statistics.Scores.Add(side.TotalScore);

                    if (format != CompetitionFormat.Match)
                        continue;

                    var isLeader =
                        side.TotalScore == maximumScore;


                    if (isLeader && leaderCount == 1)
                    {
                        statistics.Wins++;
                        statistics.TournamentPoints += winPoints;
                    }
                    else if (isLeader)
                    {
                        statistics.Draws++;
                        statistics.TournamentPoints += drawPoints;
                    }
                    else
                    {
                        statistics.Losses++;
                        statistics.TournamentPoints += lossPoints;
                    }

                }
            }
        }

        var ranking = teams.Values
            .Select(statistics => new TeamRankingResponse
            {
                TeamId = statistics.TeamId,
                TeamName = statistics.TeamName,

                Played = statistics.Scores.Count,

                RankingValue = CalculateRankingValue(
                    rankingMethod,
                    statistics),

                TotalScore = statistics.Scores.Sum(),

                Wins = statistics.Wins,
                Draws = statistics.Draws,
                Losses = statistics.Losses
            })
            .OrderByDescending(x => x.RankingValue)
            .ThenBy(x => x.TeamName,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.TeamId)
            .ToList();

        // Если рейтинг одинаковый, команды
        // занимают одинаковое место.
        // Пример: 1, 1, 3, 4.
        decimal? previousValue = null;
        var currentPosition = 0;

        for (var i = 0; i < ranking.Count; i++)
        {
            var item = ranking[i];

            if (previousValue is null ||
                item.RankingValue != previousValue.Value)
            {
                currentPosition = i + 1;
            }

            item.Position = currentPosition;
            previousValue = item.RankingValue;
        }

        return ranking;
    }

    private static decimal CalculateRankingValue(
        RankingMethod method,
        TeamStatistics statistics)
    {
        return method switch
        {
            RankingMethod.BestResult =>
                statistics.Scores.Max(),

            RankingMethod.SumResults =>
                statistics.Scores.Sum(),

            RankingMethod.AverageResults =>
                decimal.Round(
                    statistics.Scores.Average(),
                    2,
                    MidpointRounding.AwayFromZero),

            RankingMethod.MatchPoints =>
                statistics.TournamentPoints,

            _ => throw new InvalidOperationException(
                "Неизвестный способ формирования рейтинга.")
        };
    }

    private static void ValidateRankingMethod(
        CompetitionFormat format,
        RankingMethod method)
    {
        var valid = format switch
        {
            CompetitionFormat.Run =>
                method is RankingMethod.BestResult
                    or RankingMethod.SumResults
                    or RankingMethod.AverageResults,

            CompetitionFormat.Match =>
                method == RankingMethod.MatchPoints,

            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                "Способ рейтинга не соответствует " +
                "формату дисциплины.");
        }
    }

    private static void ValidateMatch(
        CompetitionFormat format,
        MatchResultResponse match)
    {
        var valid = format switch
        {
            CompetitionFormat.Run =>
                match.Sides.Count == 1,

            CompetitionFormat.Match =>
                match.Sides.Count is >= 2 and <= 4,

            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                $"Некорректное количество сторон " +
                $"в выступлении {match.MatchId}.");
        }
    }
}
