# LaneLog project state

Last updated: 2026-11-03  
Milestone: Week 2 models and local storage

## Confirmed scope

- LaneLog is a .NET 11 MAUI bowling tracker for Android. It records each game's final score and notes, not individual rolls.
- The Week 1 proposal calls for Home, Add/Edit Game, and a future Settings screen. Home currently contains the game count, average, and best score; there is not a separate Statistics screen.
- A score-photo camera/media integration is required and planned for Week 3. It is not implemented yet.

## Current implementation

- [BowlingGame](../src/LaneLog.Core/Models/BowlingGame.cs) is a seven-property SQLite model with an auto-incremented ID and a reserved nullable photo path.
- [GameDatabase](../src/LaneLog.Core/Services/GameDatabase.cs) creates the table and asynchronously reads, inserts, or updates games in `lanelog.db3` under the app's private data directory. Delete is not implemented.
- [Home](../src/LaneLog.App/Views/HomePage.xaml) displays saved games and score summaries. [Add/Edit](../src/LaneLog.App/Views/GameEditorPage.xaml) validates a 0-300 whole-number score and uses a separate draft.
- Add/Edit offers saved alley names, a new-alley entry, and No alley. New names are saved with a game; choices are derived from games rather than a separate alley table.
- Shell navigation passes a stable game ID for editing. Cancel and Android Back discard unsaved draft changes. Home refreshes after returning from the editor.
- Photo selection/capture, Settings/default alley, and delete remain future work.

## Environment and evidence

- Windows; .NET SDK `11.0.100-preview.7.26381.103`; Android target `net11.0-android`; Debug configuration.
- Android emulator: `MAUI_Emulator_API_36`, Android 16/API 36.
- `CommunityToolkit.Mvvm` 8.4.0 and `sqlite-net-pcl` 1.11.285.
- Android build succeeded with 0 warnings and 0 errors. The test run passed 12 cases: 11 validation/statistics cases and one temporary SQLite close/reopen integration test.
- On the emulator, two games were created, the second was edited from 200 to 250 without changing its ID, and both were present after a full app restart. Count 2, average 175.0, and best 250 were displayed.
- After the saved-alley update, the Android Debug build succeeded with 0 warnings and 0 errors; 13 tests passed, including two SQLite integration tests. A separate Android 16 test emulator exercised new and saved choices, editing, validation, cancel, keyboard layout, and restart without changing the existing emulator's data.
- See the [Week 2 update](week-02-update.md), [verification log](verification/week-02/verification-log.md), and [assignment PDF](Lewis_Matt_Week2_ModelsAndStorage.pdf) for details and evidence.

## Decisions and observed departures

- SQLite follows the Local Notes coursework: one asynchronous database service and an app-data file. The editor uses a fresh draft and rejects an update affecting no row so an invalid ID is not treated as a successful save.
- The Android preview handler left the activity indicator visible after the bound `IsRunning` and `IsVisible` values had both become false. A bound parent visibility boundary preserves the intended loading state. With a temporary test delay, the indicator appeared during loading and disappeared afterward; the delay is not in the app.
- The editor uses Android resize and safe-area handling because the on-screen keyboard initially covered its actions. Scrolling with the keyboard open was rechecked on the emulator.
- A Debug APK installed alone lacked Fast Deployment runtime files. `dotnet build -t:Run` deployed it correctly. An incremental resource-ID mismatch was resolved with a targeted rebuild, without uninstalling or clearing app data.

## Next actions

1. Implement and verify the actual photo picker/camera workflow, durable app-owned file, preview, and saved association.
2. Exercise media cancellation, denied/unavailable access, missing files, and relaunch on Android.
3. Implement the proposed Settings screen and complete broader accessibility, large-text, and error-path checks.
