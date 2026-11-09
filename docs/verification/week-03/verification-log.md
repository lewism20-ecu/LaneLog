# Week 3 verification log

- Date: 2026-11-09
- Revision: uncommitted Week 3 work
- Environment: Windows, .NET SDK `11.0.100-preview.7.26381.103`, `net11.0-android`, Debug; `MAUI_Emulator_API_36`, Android 16/API 36.
- Packages: `CommunityToolkit.Mvvm` 8.4.0; `sqlite-net-pcl` 1.11.285; extension-injected DevFlow agent `0.1.0-preview.12.26368.2`.

| Check | Expected | Observed | Status | Evidence |
|---|---|---|---|---|
| Android build | App compiles with the extension's DevFlow integration. | A Debug `net11.0-android`, `android-x64` build with the extension's injected targets succeeded with 0 warnings and 0 errors. | Passed | Build output in the development session |
| Core tests | Existing tests and new photo-storage tests pass. | `dotnet test tests\LaneLog.Core.Tests\LaneLog.Core.Tests.csproj --no-restore`: 18 passed, 0 failed. Five new tests cover unique copies, path safety, partial-copy cleanup, and SQLite photo references across reopening. | Passed | [Tests](../../../tests/LaneLog.Core.Tests/PhotoFileStoreTests.cs) |
| Editor and picker launch | Photo actions appear and the system picker starts. | **Choose photo** and **Take photo** appeared in the Android visual tree. Tapping **Choose photo** left the editor busy with the draft intact; Android reported its system photo picker as the foreground activity launched from LaneLog. The in-process screenshot does not show the system picker. | Partially passed | [Editor awaiting picker result](01-picker-pending.png) |
| Rendered input contrast | Inputs are legible in the checked light theme. | Score Entry and Notes Editor reported text `#000000`, transparent control backgrounds over white borders, and placeholder `#60756E`. Date and alley pickers reported text `#212121` over transparent backgrounds; the screenshot showed white parent borders. | Passed for this light-theme state | [Editor](01-picker-pending.png); runtime properties checked before the picker opened |
| Select/capture and preview | A real selected or captured image appears in the editor. | The picker was launched, but a photo has not yet been selected or captured. | Not run | A synthetic score-sheet image was added to the emulator for the next check |
| Save and restart | The game and app-owned photo survive save, reopen, and a full app restart. | The portable SQLite and file tests passed; the Android journey has not been exercised. | Not run | Future Android screenshots |
| Cancel, deny, replace, remove, missing file | Draft and saved data remain safe, and the user receives useful feedback. | Code paths exist; these Android interactions have not been exercised. | Not run | Future Android checks |
| Picker interruption | A destroyed process recovers the result and draft. | Recovery is not implemented. Stopping the debug session while the system picker was open left the picker foregrounded with the original app process gone. | Not implemented | Android activity-state observation |
| Dark mode, large text, physical device | New controls remain usable in these conditions. | Not checked for Week 3. | Not run | Future checks |

## Problems, limits, and next checks

- The first two extension launch attempts reported a build failure without useful debug output. A separate build using the extension's actual injected targets succeeded; a later extension launch succeeded.
- The launched app's DevFlow agent initially did not register because the Android broker reverse tunnel was missing. A targeted broker reverse and the agent-port forward allowed `maui_status` and `maui_tree` to reach the app.
- The DevFlow screenshot shows LaneLog's own window, not the Android system picker. The pending-picker image must not be presented as a screenshot of the picker or of a selected photo.
- The temporary system-picker activity remained after the debug session was stopped. Dismiss it on the emulator before starting a fresh debug session. The synthetic `lanelog-week3-test-b022f279.png` image is indexed by Android, but has not yet been selected.
- Verify the actual photo, camera/permission, cancel, replacement/removal, missing-file, and restart paths. Add labeled screenshots before submitting the Week 3 PDF.
