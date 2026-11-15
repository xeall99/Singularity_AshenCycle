# Skill Content Manager CRUD milestone

Session: 3 October 2026. Unity 6000.6.0f1. This milestone adds a local SQLite skill-record CRUD demonstration required by the GDD. It does not change the content schema, scene assets, packages, or player/enemy art.

## Implemented behavior

- Main Menu has a **CONTENT MANAGER** entry. Its panel lists skill records, filters by name/code, sorts in either direction, paginates, and shows an empty result when no record matches.
- The shipped **Shadow Strike** record stays read-only. The editor can create custom records with a unique name, mana cost 0–10, and nonnegative power (UI cap 999). It validates input, requires confirmation for update/delete, and refreshes the list after writes.
- Custom records are stored in the runtime copy at `Application.persistentDataPath/Database/game_data.db`; the StreamingAssets seed stays unchanged. The Editor no longer overwrites an existing runtime copy when Battle starts. Repository writes use parameterized queries where needed, SQLite transactions, and foreign-key enforcement per connection.
- Content editing is read-only while a usable active-run checkpoint exists. This prevents an active expedition from observing a skill definition change partway through a run. Custom demonstration records are not automatically equipped in combat; the editor says so explicitly.
- Database read/write failures appear in the panel rather than silently reporting success.

## Automated verification

- Final isolated Unity Play Mode suite: **151/151 PASS**, 0 failed/skipped, 123.2389302 seconds. `playmode-results.xml` and `unity-playmode.log` are in this folder.
- The new CRUD tests cover Create, Read, Update, Delete, cancellation of a confirmation, disk readback, validation, protection of the core skill, and the active-run write guard. The Main Menu test checks the new entry.
- `source-hashes.csv` confirms SHA-256 equality for all seven changed source/meta files across the workspace, isolated test copy, and final build copy. The initial test attempt failed one fixture because its setup deliberately had an active checkpoint; the fixture was corrected and the full final suite passed.
- Main Menu layout was inspected at 1920×1080 and 1280×1024 (`main-menu-*.png`). The first Windows smoke found that disabled panel buttons looked too bright. `ContentManagerUI` now uses a dark disabled state and muted button label; the full suite passed after that correction.

## Windows standalone verification

The first fresh Windows x86_64 Development build succeeded with 0 errors / 515 warnings. Its isolated profile was `CodexGateTests/SYNGRAVA_ContentManagerGate_20261003`, separate from the player's normal save. Normal input created **UKK Test Ember** with Mana 3 and Power 24. A direct SQLite read in that profile found the built-in Shadow Strike plus the new custom record. The first Player.log recorded 0 missing scripts, 0 exceptions/crash messages, and 0 Content Manager write failures. It is a diagnostic build because its disabled buttons were visually misleading.

Final Windows x86_64 Development build: `Builds/Verification-2026-10-03-ContentManagerFinal/SYNGRAVA.exe`. Fresh isolated build **PASS**, 0 errors / 515 warnings, 332,458,390-byte player, BuildPipeline 818.39 seconds. Profile identity in `SYNGRAVA_Data/app.info` is `CodexGateTests/SYNGRAVA_ContentManagerGate_20261003`; the test profile does not use the player's normal save. Gameplay assembly SHA-256 is `4F5AAA632720F5CE3F5946546C93A9BF847FBB3BC5A1F58481A596D31809CBAB`; bootstrap executable SHA-256 is `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`. Unity's bootstrap hash is shared by other builds, so the gameplay assembly hash identifies this candidate. Build log: `Builds/Verification-2026-10-03-ContentManagerFinal/unity-build.log`.

Interactive standalone smoke at 1920×1080:

| Check | Observed result |
| --- | --- |
| Main Menu and panel | Content Manager opened from Main Menu. Panel fit the display and showed the two expected rows. Disabled actions on the protected Shadow Strike record appeared visibly dim and could not be used. |
| Existing data after rebuild | The record **UKK Test Ember** created in the earlier isolated build (Mana 3 / Power 24) loaded from the same isolated profile in the final build. |
| Update and confirmation | Changed Power from 24 to 27 through the panel. Save opened a confirmation; confirming updated the visible row and showed a success message. |
| Close/relaunch persistence | Closed the game normally, relaunched the final build, and reopened Content Manager. The panel showed **UKK Test Ember** with Mana 3 / Power 27. Direct read-only SQLite readback matched both rows. Evidence: `standalone-db-readback.json`. |
| Player log | No missing-script, exception, crash, or Content Manager write-failure messages. Normal shutdown logged Unity ComputeBuffer/D3D12 resource-cleanup diagnostics. The game was closed normally after the relaunch check. |

All seven touched source/meta files match between workspace, isolated test copy, and final build copy (`source-hashes.csv`).

## Scope and remaining acceptance

This satisfies the skill CRUD demonstration at repository, UI, automated-test, and isolated standalone-smoke levels. It does not add an equip flow for created skills, edit other content tables, or change the active-run save boundary. Exact-build human acceptance of the broader Standard Run remains a separate gate. Final player/enemy art and tutorial work remain outside this milestone. No commit or push was performed.
