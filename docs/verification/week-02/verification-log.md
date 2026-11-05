# Week 2 verification log

Date: 2026-11-03  
Revision: uncommitted Week 2 work  
Environment: Windows, .NET SDK `11.0.100-preview.7.26381.103`, `net11.0-android`, Debug; `MAUI_Emulator_API_36`, Android 16/API 36.  
Packages: `CommunityToolkit.Mvvm` 8.4.0; `sqlite-net-pcl` 1.11.285.

| Check | Expected | Observed | Status | Evidence |
|---|---|---|---|---|
| Android build and deployment | Debug app builds and launches on the emulator. | `dotnet build src\LaneLog.App\LaneLog.App.csproj -t:Run -f net11.0-android -c Debug -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk"` succeeded with 0 warnings and 0 errors. | Passed | [Home](02-history-after-relaunch.png) |
| Logic and storage tests | Validation/statistics and close/reopen tests pass. | `dotnet test tests\LaneLog.Core.Tests\LaneLog.Core.Tests.csproj --no-restore`: 12 passed, 0 failed, 0 skipped; 11 pure validation/statistics cases and one real temporary SQLite integration case. | Passed | [Tests](../../../tests/LaneLog.Core.Tests/) |
| Create and read | Two new games appear with distinct IDs. | Created scores 100 and 200 with different IDs; both appeared on Home. | Passed | [Create form](00-create-before-save.png), [SQL rows after edit](05-sqlite-query.json) |
| Update and identity | Editing the second game updates rather than duplicates it. | Changed ID 2 from 200 to 250; count remained 2, average became 175.0, best became 250; note remained attached. | Passed | [Edit](01-data-entry.png), [SQL rows](05-sqlite-query.json) |
| Full restart and database read | Saved values remain without uninstalling or clearing data. | Force-stopped and relaunched the same app; both games and summary remained. A subsequent read-only query of a copy of the app-private database returned IDs 1 and 2 and the expected scores and notes. | Passed | [Home after restart](02-history-after-relaunch.png), [query and rows](05-sqlite-query.json), [read-only viewer](06-sqlite-view.png) |
| Score validation and cancel | Invalid score does not save; cancel/back discard the draft. | Score 301 showed a validation message; correction cleared it. Cancel and Android Back discarded unsaved changes in the exercised flows. | Passed | [Invalid score](03-invalid-score.png) |
| Keyboard layout | Save and Cancel remain reachable with the keyboard open. | After scrolling, both controls were visible above the Android keyboard. | Passed | [Keyboard screenshot](04-keyboard-scroll.png) |
| Loading indicator | Loading is indicated while busy and clears afterward. | A temporary 15-second load delay showed one native indicator and disabled Add while busy; after loading, no indicator remained, Add was enabled, and 2/175.0/250 appeared. The delay was removed and the final relaunch was checked again. | Passed | [Final Home](02-history-after-relaunch.png) |
| Light/dark status bar | Status icons remain legible across checked theme changes. | Icons were checked on the emulator in both light and dark configurations. | Passed | Runtime observation |
| Photo integration | Actual selected/captured photo survives save and restart; cancel/denial are safe. | Photo feature is not implemented. | Not run | Week 3 work |
| Storage-failure and invalid-route UI | Error feedback is recoverable and draft remains available. | Code paths exist, but deliberate Android failure/invalid-route exercises were not performed. | Not run | Future check |
| Large text, accessibility, other devices | UI remains usable across relevant settings and devices. | No comprehensive large-text, screen-reader, or physical-device pass was performed. | Not run | Future check |

## Problems and resolutions

- A standalone Debug APK initially failed because it did not include Fast Deployment runtime files. Deploying through the MAUI `-t:Run` target worked.
- An incremental deployment after Android resource changes produced a Shell fragment resource-ID mismatch. A targeted rebuild followed by deployment fixed the observed issue without uninstalling or clearing app data.
- The Android keyboard initially obscured form actions. Android resize and safe-area layout changes made the controls reachable by scrolling.
- The Android preview handler showed a native loading indicator even when the bound managed `IsRunning` and `IsVisible` values were false. A visibility-bound parent hides the indicator after loading; both the busy and idle states were exercised with a temporary delay, then the delay was removed.

The two sample games remain in the emulator's app data. Photo functionality, Settings, delete, and unlisted device configurations must not be reported as completed.

## Follow-up: Saved alley choices

Date: 2026-11-03. These checks used a separate temporary Android 16/API 36 emulator, leaving the original sample data untouched.

| Check | Observed | Status |
|---|---|---|
| Android build and deployment | The Debug Android build succeeded with 0 warnings and 0 errors; `-t:Run` deployed the app to the temporary emulator. | Passed |
| Logic and storage tests | `dotnet test tests\LaneLog.Core.Tests\LaneLog.Core.Tests.csproj --no-restore`: 13 passed, including two temporary SQLite integration tests. | Passed |
| New and saved alleys | An empty history offered No alley and Add a new alley. A newly named alley appeared in the next game's choices after saving and could be selected and saved again. | Passed |
| Validation and draft | A blank new name showed an error; typing cleared it. Switching choices preserved the new-name draft, and Cancel discarded it. Saving with No alley also worked. | Passed |
| Edit and choices | Editing selected the game's saved alley; Cancel kept it unchanged. Saving a different alley updated the existing game. Case-only duplicate names produced one choice without changing an older game's spelling. Updating the last game using a name removed it from the choices. | Passed |
| Restart | After force-stopping and relaunching the app without clearing data, both saved games and their alley choices reappeared. | Passed |
| Layout and themes | The editor and picker were inspected in light and dark modes. With the new-name keyboard open, scrolling reached Save and Cancel. | Passed |

These are follow-up runtime observations, not replacements for the original assignment screenshots or PDF.
