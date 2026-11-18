# Week 4 verification log

- Date: 2026-11-16
- Revision: uncommitted Week 4 UI changes
- Environment: Windows; `.NET SDK 11.0.100-preview.7.26381.103`; `net11.0-android`; Debug; `MAUI_Emulator_API_36`, Android 16.

| Check | Expected | Observed | Status | Evidence |
|---|---|---|---|---|
| Android app launch | LaneLog starts on the Android target. | The MAUI debug tool reported a successful launch on `MAUI_Emulator_API_36`. | Passed | Debug tool result |
| Hot Reload | Updated XAML applies to the running app. | Hot Reload reported that changes were applied successfully. | Passed | Hot Reload tool result |
| Visual tree and screenshot | Updated Home and game card are visible and laid out correctly. | No DevFlow agent connected. A wait timed out, and visual-tree inspection returned no tree. No current screenshot was captured. | Not run | DevFlow diagnosis below |
| Rendered accessibility properties | Input text, placeholder, and background colors have readable contrast. | Not checked against the current running build. The Week 3 log has earlier light-theme property observations. | Not run | [Week 3 verification log](../week-03/verification-log.md) |
| Screen size and orientation | Home and editor remain usable at narrow, wide, and landscape sizes. | No multi-size or orientation check was completed in this pass. | Not run | Follow-up required |
| Screen-reader navigation | Fields and the game card announce useful descriptions. | Semantic properties were added in XAML, but TalkBack was not exercised. | Not run | Follow-up required |
| Unit tests | Existing Core tests still pass. | Not rerun; this change touched only MAUI XAML and code-behind for the new ContentView. | Not run | Earlier results are in the Week 3 log |

## DevFlow connection failure

The debug launch reported `devFlowConnected: false`. A single `maui_wait` timed out; the subsequent agent list was empty and the visual-tree request returned no tree. Diagnostics reported a running broker but zero agents. I restored the reported Android reverse tunnel and confirmed it in the targeted ADB reverse list, but diagnostics still reported no agent. I stopped troubleshooting after confirming that the app itself remained launched and Hot Reload worked.

At the time of the November 16 check, the screenshots in the Week 4 update were prior Week 2 and Week 3 evidence. The November 18 follow-up below records fresh captures and additional checks.

## November 18 follow-up

- Environment: `MAUI_Emulator_API_36`, Android 16, `net11.0-android`; physical emulator size 1080 x 2400 at 420 dpi.
- The screenshots below are fresh captures from the running app. The Home entry is synthetic demo data added for this evidence; no existing app data was cleared.

| Check | Expected | Observed | Status | Evidence |
|---|---|---|---|---|
| Current Home and reusable card | Home shows the new card and score summaries. | One game with score 187; average 187.0 and best 187. The card shows the score, date, and Edit cue. | Passed | [01-home-game-card.png](01-home-game-card.png), [05-history-card-detail.png](05-history-card-detail.png) |
| Current editor form | The editor displays the entered score and notes with its labels and actions. | Score 187 and note “Practice session” were visible in the Add Game form. | Passed | [02-editor-entry.png](02-editor-entry.png) |
| Score validation feedback | A score over 300 is rejected with a clear message. | Saving a draft score of 301 displayed “Enter a whole-number score from 0 to 300.” The draft was corrected before the demo game was saved. | Passed | [04-invalid-score.png](04-invalid-score.png) |
| Compact emulator viewport | Home remains readable without clipping at a narrower size. | At a temporary 880 x 1900 resolution (about 335 x 724 device-independent units), the heading wrapped onto two lines and the summaries, Add game button, and history card remained visible. The original 1080 x 2400 size was restored. | Passed | [03-compact-layout.png](03-compact-layout.png) |
| DevFlow connectivity | The connected agent can report status and capture the running UI. | The complete diagnostic found one registered agent and matching CLI/agent versions, but the Android broker reverse tunnel on port 19223 was missing. After restoring `adb reverse tcp:19223 tcp:19223`, `maui_status` and screenshot capture succeeded. | Passed after recovery | Diagnostic output; ADB reverse list |
| Visual tree | The current Home visual tree can be inspected. | The request `maui_tree` with `{"elementId":"GameList","depth":12}` returned `MCP server 'mauidevflow-mcp': An error occurred invoking 'maui_tree'.` No tree result was recorded. | Not verified | DevFlow response |
| Rendered colors and TalkBack | Current contrast and screen-reader announcements are checked. | Not checked in this follow-up. Previous light-theme observations are in the Week 3 log; semantic descriptions in XAML do not confirm TalkBack behavior. | Not run | Follow-up required |
| Core tests and photo flow | Tests and Android photo selection/camera behavior pass. | Neither was rerun or exercised in this follow-up. | Not run | Follow-up required |
| Submission PDF | The report includes all required sections and current screenshot evidence. | Regenerated as an 8-page Letter PDF with five embedded screenshots. The reflection falls within the required 250–500-word range. The HTML print layout was rendered for visual inspection. | Passed | [Lewis_Matt_Week4_UIPolish.pdf](../../Lewis_Matt_Week4_UIPolish.pdf) |

The compact-size change was made with a temporary Android `wm size` override and restored to the original physical size afterward. This is one emulator tested at two resolutions, not two physical devices. No app data reset was performed.
