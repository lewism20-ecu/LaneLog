# LaneLog design document

Status: Week 2 implementation in progress  
Updated: 2026-11-03

## Purpose and scope

LaneLog helps a bowler keep final scores, notes, and eventually score photos together, and see an average and best score over time. Version 1 records the completed score rather than calculating individual rolls. The demonstration target is an Android phone or emulator.

## Screens

| Screen | Purpose | Current status |
|---|---|---|
| Home | List saved games, show count/average/best, and open Add or Edit. | Implemented and exercised on Android. |
| Add/Edit Game | Enter date, score and notes; choose an optional saved or new alley; save or cancel a separate draft. | Implemented and exercised on Android. |
| Settings | Configure preferences such as a default alley. | Proposed in the Week 1 design; not implemented. |

Photo selection, preview, and persistence are planned for the editor. Home already includes basic statistics; a separate Statistics screen is not part of the current proposal.

## Data and storage

The [BowlingGame model](../src/LaneLog.Core/Models/BowlingGame.cs) stores `GameId`, `PlayedOn`, `TotalScore`, `AlleyName`, `Notes`, `PhotoRelativePath`, and `UpdatedUtc`. The ID is the generated SQLite primary key. Scores must be whole numbers from 0 through 300; alley and notes are optional. The photo path is reserved for the future integration and is not yet populated by the app.

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

The planned photo feature must select or capture a real image, copy it into app-owned storage, save its relative path, and reload it after restarting. Cancellation, access denial, and a missing file should not destroy the draft or saved game. None of those media paths has been implemented or verified yet.

The current editor rejects invalid scores, retains the draft when saving fails, and reports a recoverable message. The history page reports load failures and offers Retry. Invalid-score feedback was exercised on Android; storage-failure and malformed-route UI paths have not yet been deliberately exercised.

## Verification and next milestone

The [Week 2 verification log](verification/week-02/verification-log.md) separates build, test, database, and Android checks. Week 3 will focus on real photo integration and its cancellation, permission, and persistence behavior.
