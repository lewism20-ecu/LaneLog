using LaneLog.Core.Models;

namespace LaneLog.Core.Services;

public readonly record struct GameStatistics(int Count, double? Average, int? Best);

public static class GameStatisticsCalculator
{
    public static GameStatistics Calculate(IEnumerable<BowlingGame> games)
    {
        ArgumentNullException.ThrowIfNull(games);

        var count = 0;
        long total = 0;
        int? best = null;

        foreach (var game in games)
        {
            count++;
            total += game.TotalScore;
            best = best is null ? game.TotalScore : Math.Max(best.Value, game.TotalScore);
        }

        return count == 0
            ? new GameStatistics(0, null, null)
            : new GameStatistics(count, (double)total / count, best);
    }
}
