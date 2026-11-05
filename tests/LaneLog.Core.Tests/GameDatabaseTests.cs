using LaneLog.Core.Models;
using LaneLog.Core.Services;

namespace LaneLog.Core.Tests;

public sealed class GameDatabaseTests
{
    [Fact]
    public async Task SavedAlleyNamesAreTrimmedUniqueAndAvailableAfterReopening()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lanelog-{Guid.NewGuid():N}.db3");
        GameDatabase? database = null;

        try
        {
            database = new GameDatabase(path);
            Assert.Empty(await database.GetAlleyNamesAsync());

            var games = new[]
            {
                new BowlingGame { TotalScore = 100, PlayedOn = new DateTime(2026, 10, 20), AlleyName = "River Lanes" },
                new BowlingGame { TotalScore = 120, PlayedOn = new DateTime(2026, 10, 21), AlleyName = "  Campus Lanes  " },
                new BowlingGame { TotalScore = 150, PlayedOn = new DateTime(2026, 10, 22), AlleyName = "campus lanes" },
                new BowlingGame { TotalScore = 180, AlleyName = string.Empty },
                new BowlingGame { TotalScore = 200, AlleyName = "  " }
            };

            foreach (var game in games)
            {
                await database.SaveGameAsync(game);
            }

            Assert.Equal(new[] { "campus lanes", "River Lanes" }, await database.GetAlleyNamesAsync());

            await database.CloseAsync();
            database = new GameDatabase(path);
            Assert.Equal(new[] { "campus lanes", "River Lanes" }, await database.GetAlleyNamesAsync());

            games[2].AlleyName = "East Lanes";
            await database.SaveGameAsync(games[2]);
            Assert.Equal(
                new[] { "Campus Lanes", "East Lanes", "River Lanes" },
                await database.GetAlleyNamesAsync());

            games[0].AlleyName = string.Empty;
            await database.SaveGameAsync(games[0]);
            Assert.Equal(new[] { "Campus Lanes", "East Lanes" }, await database.GetAlleyNamesAsync());

            await database.CloseAsync();
            database = new GameDatabase(path);
            Assert.Equal(new[] { "Campus Lanes", "East Lanes" }, await database.GetAlleyNamesAsync());
            Assert.Equal(5, (await database.GetGamesAsync()).Count);
        }
        finally
        {
            if (database is not null)
            {
                await database.CloseAsync();
            }

            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
        }
    }

    [Fact]
    public async Task CreateReadAndUpdateSurviveClosingAndReopeningDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lanelog-{Guid.NewGuid():N}.db3");
        GameDatabase? database = null;

        try
        {
            database = new GameDatabase(path);
            var first = new BowlingGame
            {
                PlayedOn = new DateTime(2026, 10, 20),
                TotalScore = 100,
                AlleyName = "Campus Lanes",
                Notes = "First game"
            };
            var second = new BowlingGame
            {
                PlayedOn = new DateTime(2026, 10, 21),
                TotalScore = 200,
                Notes = "Before editing"
            };

            await database.SaveGameAsync(first);
            await database.SaveGameAsync(second);
            Assert.True(first.GameId > 0);
            Assert.True(second.GameId > 0);
            Assert.NotEqual(first.GameId, second.GameId);

            var originalId = second.GameId;
            var savedGame = await database.GetGameAsync(originalId);
            Assert.NotNull(savedGame);
            savedGame.TotalScore = 250;
            savedGame.Notes = "After editing";
            await database.SaveGameAsync(savedGame);
            Assert.Equal(originalId, savedGame.GameId);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                database.SaveGameAsync(new BowlingGame { GameId = 999999, TotalScore = 100 }));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                database.SaveGameAsync(new BowlingGame { TotalScore = 301 }));
            Assert.Equal(2, (await database.GetGamesAsync()).Count);

            await database.CloseAsync();
            database = new GameDatabase(path);

            var reopened = await database.GetGamesAsync();
            Assert.Equal(2, reopened.Count);
            var edited = Assert.Single(reopened, game => game.GameId == originalId);
            Assert.Equal(250, edited.TotalScore);
            Assert.Equal("After editing", edited.Notes);
            Assert.Equal(new DateTime(2026, 10, 21), edited.PlayedOn.Date);
            Assert.True(edited.UpdatedUtc > DateTime.MinValue);
        }
        finally
        {
            if (database is not null)
            {
                await database.CloseAsync();
            }

            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
        }
    }
}
