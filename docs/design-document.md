# LaneLog design document

Status: Week 3 photo integration implemented; Android verification in progress

Updated: 2026-11-09

## Purpose and scope

LaneLog helps a bowler keep final scores, notes, and optional score photos together, and see an average and best score over time. Version 1 records the completed score rather than calculating individual rolls. The demonstration target is an Android phone or emulator.

## Screens

| Screen | Purpose | Current status |
|---|---|---|
| Home | List saved games, show count/average/best, and open Add or Edit. | Implemented and exercised on Android. |
| Add/Edit Game | Enter date, score and notes; choose an optional alley and score photo; save or cancel a separate draft. | Game entry exercised on Android; photo controls rendered, but the complete media journey is not yet verified. |
| Settings | Configure preferences such as a default alley. | Proposed in the Week 1 design; not implemented. |

The editor now includes photo selection, capture, preview, replacement, and removal. Home already includes basic statistics; a separate Statistics screen is not part of the current proposal.

## Data and storage

The [BowlingGame model](../src/LaneLog.Core/Models/BowlingGame.cs) stores `GameId`, `PlayedOn`, `TotalScore`, `AlleyName`, `Notes`, `PhotoRelativePath`, and `UpdatedUtc`. The ID is the generated SQLite primary key. Scores must be whole numbers from 0 through 300; alley, notes, and the relative photo path are optional.

There is currently one persisted entity and no foreign-key relationship. The count, average, and best score are calculated from the saved games rather than stored as separate records.

The [database service](../src/LaneLog.Core/Services/GameDatabase.cs) uses `sqlite-net-pcl` and an asynchronous connection to an app-private SQLite file. Initialization creates the table if necessary. A zero ID inserts a game; an existing ID updates it. Loading is ordered by date and ID. This follows the coursework's Local Notes storage pattern without requiring an account or server.

The editor's alley choices come from the names on saved games rather than a separate alley table. Names are trimmed, deduplicated without regard to case, and sorted for the picker. A new name becomes available after its game is saved; a name disappears from the choices if no saved game uses it. Existing game rows keep their stored alley strings.

## Architecture and course connections

- XAML pages bind to constructor-injected CommunityToolkit.Mvvm ViewModels.
- Shell routes to a transient editor using a stable game ID; Home uses an observable collection and a typed `CollectionView` item template.
- A portable Core project holds the model, storage service, score validation, and statistics calculation. Tests run without launching the MAUI UI.
- The singleton database service owns a connection; durability comes from SQLite on disk, not the service lifetime.
- Shared styles and semantic headings support the Android UI. Broader accessibility, large-text, and device coverage are still pending.

## Integration and failure handling

The [photo service](../src/LaneLog.App/Services/PhotoService.cs) invokes MAUI's native media picker on the main thread and copies the selected stream through [PhotoFileStore](../src/LaneLog.Core/Services/PhotoFileStore.cs) into app-owned storage. The editor saves its relative path with the game. It keeps a previously saved photo until the database update succeeds, and removes an old copy only if no saved game refers to it. An unavailable photo produces a message without discarding the game. The original gallery file is never deleted.

Android requests camera access only for capture; image selection uses the installed MAUI preview's system picker or `ACTION_GET_CONTENT` fallback. App data can be included in Android's system backups, and photo metadata can remain in the copied image. The complete select/capture, cancel, denial, missing-file, replacement, and restart flows still need Android verification. Recovery of a picker result and unsaved draft after Android destroys the app process is not implemented.

The current editor rejects invalid scores, retains the draft when saving fails, and reports a recoverable message. The history page reports load failures and offers Retry. Invalid-score feedback was exercised on Android; storage-failure and malformed-route UI paths have not yet been deliberately exercised.

## Verification and next milestone

The [Week 2 verification log](verification/week-02/verification-log.md) and [Week 3 verification log](verification/week-03/verification-log.md) separate builds, tests, database checks, and Android observations. The next step is to finish the real Android media and persistence checks before claiming the integration is complete.
