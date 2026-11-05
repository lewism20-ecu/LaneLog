using System.Globalization;

namespace LaneLog.Core.Services;

public static class GameValidator
{
    public const string ScoreError = "Enter a whole-number score from 0 to 300.";

    public static bool TryParseScore(string? input, out int score)
    {
        return int.TryParse(input?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out score)
            && score is >= 0 and <= 300;
    }
}
