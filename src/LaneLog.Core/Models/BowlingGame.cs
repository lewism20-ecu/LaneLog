using SQLite;

namespace LaneLog.Core.Models;

[Table("Games")]
public sealed class BowlingGame
{
    [PrimaryKey, AutoIncrement]
    public int GameId { get; set; }

    public DateTime PlayedOn { get; set; } = DateTime.Today;

    public int TotalScore { get; set; }

    public string AlleyName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string? PhotoRelativePath { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
