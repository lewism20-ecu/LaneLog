# Week 2 status update: Models & Storage

Matt Lewis | LaneLog | November 3, 2026

## 1. Data model documentation

`BowlingGame` represents one completed game. The SQLite `Games` table stores one row per game. Its seven properties support history, editing, score summaries, and the planned photo feature.

| Property | Type | Purpose |
|---|---|---|
| `GameId` | `int` | Auto-incremented primary key; keeps an edited game associated with the same record. |
| `PlayedOn` | `DateTime` | Date the game was played; defaults to today for a new draft. |
| `TotalScore` | `int` | Final whole-number score from 0 to 300. |
| `AlleyName` | `string` | Optional name of the bowling alley. |
| `Notes` | `string` | Optional notes about the game. |
| `PhotoRelativePath` | `string?` | Reserved relative path for a future score photo; photo integration is not implemented yet. |
| `UpdatedUtc` | `DateTime` | UTC timestamp set when the record is saved. |

There is one persisted model and no foreign-key relationships. Count, average, and best score are calculated from saved games. The alley picker also uses alley names from those games; it does not store alleys in a separate table. Names are trimmed, sorted, and deduplicated without regard to case. Source: [BowlingGame.cs](../src/LaneLog.Core/Models/BowlingGame.cs).

## 2. Persistent storage implementation

LaneLog uses `sqlite-net-pcl` and a single `SQLiteAsyncConnection` in [GameDatabase.cs](../src/LaneLog.Core/Services/GameDatabase.cs). SQLite matches the Local Notes coursework and keeps structured records on the device without a server or account. Dependency injection supplies the database path as `Path.Combine(FileSystem.Current.AppDataDirectory, "lanelog.db3")`; on the Android emulator this is private app storage.

The service creates the `Games` table during initialization and waits for that work before reading or saving. New records have ID 0 and are inserted; saved records are updated by their existing ID. A score outside 0-300 is rejected, and an update affecting no row is reported as an error. On Home, games are loaded from SQLite in descending played-date order and then by descending ID; the app calculates the summary from the loaded rows.

The alley picker reads names from saved games. In Add/Edit, I can select a saved name, choose Add a new alley..., or select No alley. A new name is saved with the game. It appears in future choices while at least one saved game uses it.

Relevant save and load excerpts:

```csharp
initialization = database.CreateTableAsync<BowlingGame>();

var affected = game.GameId == 0
    ? await database.InsertAsync(game)
    : await database.UpdateAsync(game);
```

```csharp
await initialization;
return await database.Table<BowlingGame>()
    .OrderByDescending(game => game.PlayedOn)
    .ThenByDescending(game => game.GameId)
    .ToListAsync();
```

One challenge was confirming that the data was saved to SQLite and not just held in memory. I fully stopped and reopened the app without uninstalling it or clearing its data. A query against a copy of the app's Android database returned two rows:

```sql
SELECT GameId, TotalScore, AlleyName, Notes
FROM Games ORDER BY GameId;
```

| GameId | TotalScore | AlleyName | Notes |
|---:|---:|---|---|
| 1 | 100 | Campus Lanes | Practiced spare shots. |
| 2 | 250 | River Lanes | Focused on timing. |

The original query and results are also saved as [SQLite evidence](verification/week-02/05-sqlite-query.json).

## 3. CRUD progress

- **Create - implemented and exercised:** Select Add game, enter a date and score, and choose a saved alley, add a new alley, or select No alley. Add optional notes and choose Save game. The app validates the score and inserts a new row.
- **Read - implemented and exercised:** Home reloads saved games and displays their scores, dates, alleys, count, average, and best. After a full restart, both sample games and their alley names remained visible.
- **Update - implemented and exercised:** Select a saved game to load a separate editor draft by `GameId`. Saving writes that same ID. The second sample was changed from 200 to 250; its ID stayed 2, and the total remained two games.
- **Delete - not implemented:** Delete is optional for this week's assignment and remains future work.
- **Validation and cancel:** Score 301 was rejected with a clear message. Correcting the score cleared the message. Cancel and Android Back discarded unsaved changes in the exercised flows.

The Android Debug build succeeded with 0 warnings and 0 errors. All 13 core tests passed, including validation, statistics, and two SQLite persistence tests. On a separate Android 16 emulator, I tried adding and reusing alley names, editing and canceling, blank-name validation, and checking the choices after restarting the app. Details and limits are in the [verification log](verification/week-02/verification-log.md).

## 4. Screenshots and evidence

The alley-choice checks used a separate Android 16/API 36 emulator so the two original sample games were left untouched. The earlier screenshots show the first create, edit, validation, and restart checks; the current add-game screenshots show the alley picker.

1. [New alley entry](verification/week-02/07-new-alley-entry.png): a draft with the new-alley name field open.
2. [Saved alley choices](verification/week-02/08-saved-alley-choices.png): the picker offers saved names as well as No alley and Add a new alley.
3. [Create form before save](verification/week-02/00-create-before-save.png): score 200, River Lanes, and a note entered for the second game.
4. [Edit form](verification/week-02/01-data-entry.png): the same game reopened with its updated score of 250 and saved note.
5. [Home after full restart](verification/week-02/02-history-after-relaunch.png): two stored games; count 2, average 175.0, and best 250.
6. [Invalid score](verification/week-02/03-invalid-score.png): score 301 is rejected with a visible explanation.
7. [Keyboard and scrolling](verification/week-02/04-keyboard-scroll.png): Save and Cancel remain reachable while the Android keyboard is open.
8. [SQLite query viewer](verification/week-02/06-sqlite-view.png): a read-only viewer displays the actual rows from a copy of the app-private database.

The SQLite query and its actual rows above provide accessible text alongside the database screenshot.

## 5. Reflection

Editing a game without accidentally adding a second row was the hardest part. I followed its ID from Home into the editor and checked the same ID after saving. Looking at the form wasn't enough; I needed to know the data was on the device. I force-stopped and reopened the app, then queried a copy of its SQLite database. The two game IDs and their saved scores were still there.

I chose SQLite because the Local Notes coursework used an asynchronous database service, and LaneLog stores a small set of game records in the app's private data folder. New games are inserted, saved rows are updated by ID, and the editor and database service both check the score. I kept the editor separate from Home so Cancel or Android Back does not change saved games.

I'm most satisfied that I can add and edit games and see the same records after a restart. I entered scores of 100 and 200, changed the second score to 250, and checked that the count stayed at two, the average was 175.0, and the best was 250. I also fixed two problems on the emulator: the keyboard covered the form buttons, and the loading indicator stayed up after Home finished loading. I checked both fixes on Android.

For Week 3, I plan to add the score-photo feature from the proposal. I want to choose or take a picture, save a copy in the app, and link it to a game so it can be opened after a restart. I also need to test canceling, missing files, and camera or permission failures without losing the game draft. The Settings screen, large-text checks, and broader accessibility checks are still ahead.

## Next week

Implement and test real photo selection/capture, preview, durable app-owned storage, saved association, cancellation, and failure handling. Keep Settings and broader UI checks separate from completed Week 2 work.

## Follow-up: Saved alley choices

After the original Week 2 evidence was captured, the editor was updated to offer names from saved games, an option to add a new name, and No alley. A new name is saved with its game; there is no separate alley table. Names are removed from the choices when no saved game uses them.

The updated Android Debug build succeeded, and all 13 core tests passed. On a separate Android 16 emulator, I checked adding and reusing names, editing and canceling, blank-name validation, and choices after restarting the app. The first two screenshots above show the updated picker; the remaining history and edit screenshots show the original Week 2 sample data.
