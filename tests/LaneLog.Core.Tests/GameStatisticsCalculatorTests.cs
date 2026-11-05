using LaneLog.Core.Models;
using LaneLog.Core.Services;

namespace LaneLog.Core.Tests;

public sealed class GameStatisticsCalculatorTests
{
    [Fact]
    public void StatisticsUseAllScoresWithoutIntegerTruncation()
    {
        var games = new[]
        {
            new BowlingGame { TotalScore = 100 },
            new BowlingGame { TotalScore = 151 },
            new BowlingGame { TotalScore = 201 }
        };

        var result = GameStatisticsCalculator.Calculate(games);

        Assert.Equal(3, result.Count);
        Assert.Equal(452.0 / 3, Assert.IsType<double>(result.Average), 10);
        Assert.Equal(201, result.Best);
    }

    [Fact]
    public void EmptyHistoryHasNoInventedAverageOrBest()
    {
        var result = GameStatisticsCalculator.Calculate(Array.Empty<BowlingGame>());

        Assert.Equal(0, result.Count);
        Assert.Null(result.Average);
        Assert.Null(result.Best);
    }
}
