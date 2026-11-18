# LaneLog project state

Last updated: 2026-11-18

Milestone: Week 4 UI polish; runtime verification partially complete

## Confirmed scope

- LaneLog is a .NET 11 MAUI bowling tracker for Android. It records each game's final score and notes, not individual rolls.
- The Week 1 proposal calls for Home, Add/Edit Game, and a future Settings screen. Home currently contains the game count, average, and best score; there is not a separate Statistics screen.
- A score-photo camera/media integration is required. Its implementation is present; end-to-end Android verification is still pending.

## Current implementation

- [BowlingGame](../src/LaneLog.Core/Models/BowlingGame.cs) is a seven-property SQLite model with an auto-incremented ID and a nullable photo path.
- [GameDatabase](../src/LaneLog.Core/Services/GameDatabase.cs) creates the table and asynchronously reads, inserts, or updates games in `lanelog.db3` under the app's private data directory. Delete is not implemented.
- [Home](../src/LaneLog.App/Views/HomePage.xaml) displays saved games and score summaries. [Add/Edit](../src/LaneLog.App/Views/GameEditorPage.xaml) validates a 0-300 whole-number score and uses a separate draft.
- Add/Edit offers saved alley names, a new-alley entry, and No alley. New names are saved with a game; choices are derived from games rather than a separate alley table.
- Shell navigation passes a stable game ID for editing. Cancel and Android Back discard unsaved draft changes. Home refreshes after returning from the editor.
- [PhotoService](../src/LaneLog.App/Services/PhotoService.cs) uses the native picker/camera; [PhotoFileStore](../src/LaneLog.Core/Services/PhotoFileStore.cs) copies selected streams into app-owned storage. The editor supports preview, replacement, and removal, with the old saved file retained until a successful database update.
- [GameHistoryCard](../src/LaneLog.App/Views/GameHistoryCard.xaml) is a reusable card used by Home's typed CollectionView template. Home and the editor have centered maximum content widths of 840 and 680 device-independent units. The editor's notes field grows with its content, and its input controls have semantic descriptions.
- Settings/default alley and delete remain future work.

## Environment and evidence

- Windows; .NET SDK `11.0.100-preview.7.26381.103`; Android target `net11.0-android`; Debug configuration.
- Android emulator: `MAUI_Emulator_API_36`, Android 16/API 36.
- `CommunityToolkit.Mvvm` 8.4.0 and `sqlite-net-pcl` 1.11.285.
- Android build succeeded with 0 warnings and 0 errors. The test run passed 12 cases: 11 validation/statistics cases and one temporary SQLite close/reopen integration test.
- On the emulator, two games were created, the second was edited from 200 to 250 without changing its ID, and both were present after a full app restart. Count 2, average 175.0, and best 250 were displayed.
- After the saved-alley update, the Android Debug build succeeded with 0 warnings and 0 errors; 13 tests passed, including two SQLite integration tests. A separate Android 16 test emulator exercised new and saved choices, editing, validation, cancel, keyboard layout, and restart without changing the existing emulator's data.
- See the [Week 2 update](week-02-update.md), [verification log](verification/week-02/verification-log.md), and [assignment PDF](Lewis_Matt_Week2_ModelsAndStorage.pdf) for details and evidence.
- Week 3: the Android Debug build with DevFlow injection succeeded with 0 warnings and 0 errors. All 18 core tests passed, including five new file-copy, path-safety, and SQLite/photo-reference checks.
- On the Android 16 emulator, the editor and photo buttons rendered, and tapping **Choose photo** launched Android's system photo picker. A selected image, camera capture, preview, cancellation, permissions, and persistence after an Android restart have not yet been verified. See the [Week 3 update](week-03-update.md) and [verification log](verification/week-03/verification-log.md).
- Week 4: The Android Debug build succeeded with 0 warnings and 0 errors. On November 18, the Android 16 emulator displayed a demo game with a score of 187, average 187.0, and best 187; current Home, Add Game, and invalid-score screenshots were saved. Home also fit at a temporary 880 x 1900 resolution (about 335 x 724 device-independent units), with the heading wrapping and no visible clipping. The original 1080 x 2400 resolution was restored. DevFlow diagnostics found a missing Android broker reverse tunnel; after restoring it, the agent status and screenshots worked. One visual-tree request returned a generic MCP error. TalkBack, large text, dark-mode contrast, wide and landscape layouts, Core tests, and the Android photo flow remain unverified this week. The Week 4 PDF has five current screenshots and a reflection within the required 250–500-word range. See the [Week 4 update](week-04-update.md) and [verification log](verification/week-04/verification-log.md).

## Decisions and observed departures

- SQLite follows the Local Notes coursework: one asynchronous database service and an app-data file. The editor uses a fresh draft and rejects an update affecting no row so an invalid ID is not treated as a successful save.
- The Android preview handler left the activity indicator visible after the bound `IsRunning` and `IsVisible` values had both become false. A bound parent visibility boundary preserves the intended loading state. With a temporary test delay, the indicator appeared during loading and disappeared afterward; the delay is not in the app.
- The editor uses Android resize and safe-area handling because the on-screen keyboard initially covered its actions. Scrolling with the keyboard open was rechecked on the emulator.
- A Debug APK installed alone lacked Fast Deployment runtime files. `dotnet build -t:Run` deployed it correctly. An incremental resource-ID mismatch was resolved with a targeted rebuild, without uninstalling or clearing app data.
- For Week 3, the extension-managed `MAUI_DEVFLOW` integration was added rather than a permanent agent package reference. The Android emulator initially lacked the broker reverse tunnel; targeted DevFlow tunnel setup allowed the agent and visual tree to connect. The system picker is outside the in-process MAUI visual tree.
- MAUI's picker may be interrupted if Android destroys the process. Recovery of that operation and the unsaved editor draft is not implemented; an interrupted draft may also leave an unreferenced app-owned copy.

## Next actions

1. Check large text, screen-reader navigation, rendered colors, and wide or landscape layouts. The current compact-layout check used a second emulator resolution, not a separate device.
2. Select a synthetic score-sheet image in Android; verify preview, save, reopen, and full relaunch without clearing data. Then check camera capture, cancellation, denied or unavailable access, replacement/removal, missing files, and interrupted picker recovery.
3. Run and record the Core tests for Week 5. Settings and game deletion are still future work.
