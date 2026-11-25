
namespace RoboReglament.Server.Models;

public enum RankingMethod
{
    // Лучший результат среди всех заездов команды
    BestResult = 0,

    // Сумма результатов всех заездов команды
    SumResults = 1,

    // Средний результат всех заездов команды
    AverageResults = 2,

    // Турнирные очки за результаты матчей
    MatchPoints = 3
}
