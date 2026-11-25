
namespace RoboReglament.Server.DTOs.Results;

public sealed class TeamRankingResponse
{
    // Место в рейтинге дисциплины
    public int Position { get; set; }

    public int TeamId { get; set; }

    public string TeamName { get; set; } = string.Empty;

    // Количество завершённых выступлений команды
    public int Played { get; set; }

    // Значение, по которому определяется место
    public decimal RankingValue { get; set; }

    // Сумма всех заработанных баллов
    public decimal TotalScore { get; set; }

    // Статистика для матчевых дисциплин
    public int Wins { get; set; }

    public int Draws { get; set; }

    public int Losses { get; set; }
}
