using LaneLog.Core.Models;
using SQLite;

namespace LaneLog.Core.Services;

public sealed class GameDatabase
{
    private readonly SQLiteAsyncConnection database;
    private readonly Task initialization;

    public GameDatabase(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        database = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        initialization = database.CreateTableAsync<BowlingGame>();
    }

    public async Task<List<BowlingGame>> GetGamesAsync()
    {
        await initialization;
        return await database.Table<BowlingGame>()
            .OrderByDescending(game => game.PlayedOn)
            .ThenByDescending(game => game.GameId)
            .ToListAsync();
    }

    public async Task<List<string>> GetAlleyNamesAsync()
    {
        var games = await GetGamesAsync();
        return games
            .Select(game => game.AlleyName?.Trim())
            .OfType<string>()
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<BowlingGame?> GetGameAsync(int gameId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gameId);

        await initialization;
        return await database.FindAsync<BowlingGame>(gameId);
    }

    public async Task SaveGameAsync(BowlingGame game)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentOutOfRangeException.ThrowIfNegative(game.GameId);

        if (game.TotalScore is < 0 or > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(game), GameValidator.ScoreError);
        }

        await initialization;
        game.UpdatedUtc = DateTime.UtcNow;

        var affected = game.GameId == 0
            ? await database.InsertAsync(game)
            : await database.UpdateAsync(game);

        if (affected != 1)
        {
            throw new InvalidOperationException("The game could not be saved; it may no longer exist.");
        }
    }

    public Task CloseAsync() => database.CloseAsync();
}
