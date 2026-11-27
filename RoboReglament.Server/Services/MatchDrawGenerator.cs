
using System.Security.Cryptography;
using RoboReglament.Server.DTOs.Matches;
using RoboReglament.Server.Models;

namespace RoboReglament.Server.Services;

public static class MatchDrawGenerator
{
    private const int MaxNewMatches = 200;

    public static List<int[][]> Generate(
        CompetitionFormat format,
        int sidesPerEvent,
        int teamsPerSide,
        IReadOnlyCollection<int> eligibleTeams,
        IReadOnlyCollection<int[][]> existingMatches,
        MatchDrawMode mode,
        int appearancesPerTeam)
    {
        var teams = eligibleTeams
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (teams.Length == 0)
            throw new InvalidOperationException(
                "Нет аккредитованных команд.");

        if (sidesPerEvent <= 0 || teamsPerSide <= 0)
            throw new ArgumentException(
                "Некорректная структура выступления.");

        if (!Enum.IsDefined(mode))
            throw new ArgumentException(
                "Неизвестный способ жеребьёвки.");

        if (appearancesPerTeam is < 1 or > 30)
            throw new ArgumentException(
                "Количество выступлений должно быть от 1 до 30.");

        var slots = sidesPerEvent * teamsPerSide;

        if (teams.Length < slots)
            throw new InvalidOperationException(
                "Недостаточно аккредитованных команд.");

        var eligibleSet = teams.ToHashSet();

        var appearances = teams.ToDictionary(
            id => id, _ => 0);

        var frequency = new Dictionary<string, int>();

        foreach (var match in existingMatches)
        {
            var ids = match.SelectMany(x => x).ToArray();

            if (ids.Distinct().Count() != ids.Length ||
                ids.Any(id => !eligibleSet.Contains(id)))
            {
                throw new InvalidOperationException(
                    "Существующие выступления содержат " +
                    "повторяющиеся или недоступные команды.");
            }

            foreach (var id in ids)
                appearances[id]++;

            var key = Key(match);

            frequency[key] = frequency.GetValueOrDefault(key) + 1;
        }

        if (mode == MatchDrawMode.AllUnique)
        {
            var candidates = GetAllUnique(
                format, sidesPerEvent, teamsPerSide, teams);

            var missing = candidates
                .Where(x => !frequency.ContainsKey(Key(x)))
                .ToList();

            if (missing.Count == 0)
                throw new InvalidOperationException(
                    "Все уникальные встречи уже созданы.");

            if (missing.Count > MaxNewMatches)
                throw new InvalidOperationException(
                    "Получается слишком много матчей.");

            return missing;
        }

        // В фиксированном режиме один раунд
        // включает каждую команду ровно один раз.
        if (teams.Length % slots != 0)
        {
            throw new InvalidOperationException(
                "Для равномерной жеребьёвки число команд " +
                "должно делиться на размер выступления. " +
                "Для дуэлей с нечётным количеством команд " +
                "можно использовать режим всех встреч.");
        }

        var previousCounts = appearances.Values
            .Distinct()
            .ToArray();

        if (previousCounts.Length != 1)
        {
            throw new InvalidOperationException(
                "Существующие выступления распределены " +
                "неравномерно. Для продолжения жеребьёвки " +
                "у всех команд должно быть одинаковое " +
                "число выступлений.");
        }

        var previous = previousCounts[0];
        var additionalRounds = appearancesPerTeam - previous;

        if (additionalRounds <= 0)
        {
            throw new InvalidOperationException(
                "Указанное количество выступлений " +
                "уже достигнуто.");
        }

        if (additionalRounds * teams.Length / slots >
            MaxNewMatches)
        {
            throw new InvalidOperationException(
                "План превышает 200 новых выступлений.");
        }

        var result = new List<int[][]>();

        var standardRounds = GetStandardRounds(
            format, sidesPerEvent, teamsPerSide, teams);

        for (var roundNumber = 0;
             roundNumber < additionalRounds;
             roundNumber++)
        {
            int[][][]? best = null;
            var bestCost = int.MaxValue;

            var attempts = standardRounds is null ? 128 : 1;

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var candidates = standardRounds ??
                    new List<int[][][]>
                    {
                        RandomRound(
                            teams, sidesPerEvent, teamsPerSide)
                    };

                foreach (var candidate in candidates)
                {
                    var cost = candidate.Sum(match =>
                        frequency.GetValueOrDefault(
                            Key(match)));

                    if (cost < bestCost)
                    {
                        best = candidate;
                        bestCost = cost;
                    }
                }
            }

            if (best is null)
                throw new InvalidOperationException(
                    "Не удалось сформировать расписание.");

            foreach (var match in best)
            {
                result.Add(match);

                var key = Key(match);

                frequency[key] =
                    frequency.GetValueOrDefault(key) + 1;
            }
        }

        return result;
    }

    private static List<int[][]> GetAllUnique(
        CompetitionFormat format,
        int sides,
        int perSide,
        int[] teams)
    {
        // Одиночные заезды: по одному заезду на команду.
        if (format == CompetitionFormat.Run &&
            sides == 1 && perSide == 1)
        {
            return teams
                .Select(id => new[] { new[] { id } })
                .ToList();
        }

        if (format == CompetitionFormat.Match &&
            sides == 2 && perSide == 1)
        {
            var matches = new List<int[][]>();

            for (var i = 0; i < teams.Length; i++)
                for (var j = i + 1; j < teams.Length; j++)
                {
                    matches.Add(new[]
                    {
                    new[] { teams[i] },
                    new[] { teams[j] }
                });
                }

            return matches;
        }

        if (format == CompetitionFormat.Match &&
            sides == 2 && perSide == 2 &&
            teams.Length == 4)
        {
            return AlliancePartitions(teams)
                .Select(round => round[0])
                .ToList();
        }

        throw new ArgumentException(
            "Режим всех уникальных встреч поддерживается " +
            "для одиночных заездов, дуэлей 1 на 1 " +
            "и четырёх команд в союзах 2 на 2. " +
            "Для остальных форматов используйте " +
            "заданное число выступлений.");
    }

    private static List<int[][][]>? GetStandardRounds(
        CompetitionFormat format,
        int sides,
        int perSide,
        int[] teams)
    {
        if (format == CompetitionFormat.Run &&
            sides == 1 && perSide == 1)
        {
            return new List<int[][][]>
            {
                teams.Select(id =>
                    new[] { new[] { id } }).ToArray()
            };
        }

        if (format == CompetitionFormat.Match &&
            sides == 2 && perSide == 2 &&
            teams.Length == 4)
        {
            return AlliancePartitions(teams);
        }

        if (format == CompetitionFormat.Match &&
            sides == 2 && perSide == 1 &&
            teams.Length % 2 == 0)
        {
            return PairRounds(teams);
        }

        return null;
    }


    private static List<int[][][]> AlliancePartitions(int[] ids)
    {
        var rounds = new List<int[][][]>();

        for (var partner = 1; partner < 4; partner++)
        {
            var first = new[] { ids[0], ids[partner] };

            var second = ids
                .Where(id => !first.Contains(id))
                .ToArray();

            rounds.Add(new[]
            {
                new[] { first, second }
            });
        }

        return rounds;
    }
    private static List<int[][][]> PairRounds(int[] ids)
    {
        var rotation = (int[])ids.Clone();
        var rounds = new List<int[][][]>();

        for (var round = 0;
             round < ids.Length - 1;
             round++)
        {
            var matches = new List<int[][]>();

            for (var i = 0; i < ids.Length / 2; i++)
            {
                matches.Add(new[]
                {
                    new[] { rotation[i] },
                    new[] { rotation[ids.Length - 1 - i] }
                });
            }

            rounds.Add(matches.ToArray());

            var last = rotation[^1];

            for (var i = rotation.Length - 1; i > 1; i--)
                rotation[i] = rotation[i - 1];

            rotation[1] = last;
        }

        return rounds;
    }

    private static int[][][] RandomRound(
        int[] teams,
        int sides,
        int perSide)
    {
        var shuffled = (int[])teams.Clone();

        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);

            (shuffled[i], shuffled[j]) =
                (shuffled[j], shuffled[i]);
        }

        return shuffled
            .Chunk(sides * perSide)
            .Select(block =>
                Enumerable.Range(0, sides)
                    .Select(side =>
                        block
                            .Skip(side * perSide)
                            .Take(perSide)
                            .ToArray())
                    .ToArray())
            .ToArray();
    }

    private static string Key(int[][] match)
    {
        return string.Join("|",
            match
                .Select(side =>
                    string.Join(",",
                        side.OrderBy(id => id)))
                .OrderBy(x => x, StringComparer.Ordinal));
    }
}
