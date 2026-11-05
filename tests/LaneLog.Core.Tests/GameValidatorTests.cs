using LaneLog.Core.Services;

namespace LaneLog.Core.Tests;

public sealed class GameValidatorTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("300", 300)]
    [InlineData(" 150 ", 150)]
    public void WholeScoresWithinRangeAreAccepted(string input, int expected)
    {
        Assert.True(GameValidator.TryParseScore(input, out var score));
        Assert.Equal(expected, score);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("301")]
    [InlineData("150.5")]
    public void InvalidScoresAreRejected(string? input)
    {
        Assert.False(GameValidator.TryParseScore(input, out _));
    }
}
