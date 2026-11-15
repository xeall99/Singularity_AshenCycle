# Standard Run quality follow-up — 30 September 2026

Status: **automated/render/build PASS; manual release gate remains open**.

## Delivered behavior

- A failed checkpoint write now produces a visible footer warning on the Standard Run map: **PROGRESS NOT SAVED. Retry before closing the game.** The **RETRY SAVE** button retries the current committed map boundary. A failed retry retains the warning; a successful write clears it. The player can continue the current session while the warning is present, so it does not claim that unsaved progress has become durable.
- The retry callback rejects hidden/stale clicks and refuses to save while the map is closed, the run is terminal, or a node is still being processed. The existing checkpoint format and atomic-write implementation are unchanged.
- Run Summary now displays the seed and full RunId in its footer. The existing History failure warning is placed to the right of the main action, leaving the run identity and action readable together.
- Added a regression for destroying Settings while its pause modal is open. The existing `OnDestroy` implementation already restored time scale; a redundant edit was removed after review. This is newly verified behavior, not a newly delivered pause fix.

## Verification

| Check | Result | Evidence |
| --- | --- | --- |
| Checkpoint failure / failed retry / successful retry / hidden stale click | **1/1 PASS** | `focused-results.xml`; an isolated test path was blocked by a sentinel file. The warning stayed visible after failure, the existing checkpoint and sentinel stayed unchanged, and a successful retry wrote the active RunId/depth with `NodeEntered = false`. |
| Complete Unity Play Mode regression | **140/140 PASS** | `full-results.xml`, 0 failed/skipped, 109.9648516 seconds. Includes the existing natural combat gate, Recovery, Summary, History, score, leaderboard, and the new Settings teardown check. |
| Layout and visual review | **PASS in Editor render** | `checkpoint-warning-*` and `run-summary-*` PNGs at 1920×1080 and 1280×1024. Tests check label overflow and containment; images were visually inspected. Summary captures use the accelerated test fixture and are not human-controlled runs. |
| Source identity | **52/52 match** | `source-hashes.csv`: SHA-256 comparisons of all project scripts, MainMenu/Battle scenes, test source, and BuildScript against the isolated project. |
| Windows x86_64 Development Build | **PASS** | `Builds/Verification-2026-09-30-SaveFeedback/SYNGRAVA.exe`; Unity 6000.6.0f1 reports 0 errors, 498 warnings, 332,435,830 bytes, 64.88 seconds. Full log: `unity-build.log` in the build folder. |
| Headless player startup probe | **PARTIAL PASS** | `player-startup.log` reaches engine/domain/physics initialization and `UnloadTime`, without a recorded exception or crash. `-quit` did not end this player automatically, so the probe was stopped after observation. It verifies startup only; it does not verify menu input, normal exit, or a completed run. |
| Interactive standalone playthrough | **NOT RUN in this follow-up** | Native computer control was unavailable through the current tool surface. No human playtest credit is assigned to Editor tests or renders. |

Tests reused the previously isolated Unity project after restoring `SYNGRAVA_BattleTests` and its temporary company identity. The initial attempt had the previous build identity and was stopped; a sandboxed retry could not connect to the local Unity licensing service. The completed test run used the correct test identity and succeeded outside the sandbox. These incomplete attempts are not counted as test evidence.

Before building, only the isolated project's company/product identity was changed to `CodexGateTests/SYNGRAVA_QualityGate_20260930`. The compiled player's `SYNGRAVA_Data/app.info` confirms those values. This new profile is separate from the earlier RecoveryFix test profile and the original player profile. Save-location behavior for this exact player has not yet been exercised interactively.

## Files changed in this follow-up

- `Assets/Project/Scripts/Battle/BattleManager.cs`: checkpoint failure status and a guarded retry at the map boundary.
- `Assets/Project/Scripts/UI/StandardRunMapUI.cs`: runtime warning and Retry Save control, hidden when saving succeeds.
- `Assets/Project/Scripts/UI/RunSummaryUI.cs`: visible seed/RunId and footer placement.
- `Assets/Tests/PlayMode/BattleSnapshotTests.cs`: save failure/retry behavior and render assertions; Summary identity assertions; Settings teardown regression.
- Project context, release checklist, and this report record the verified state.

No new scene, package, Inspector assignment, save migration, or generated art is required. Existing unrelated workspace changes are preserved.

## Remaining gate

The latest automated and render evidence does **not** close the Standard Run release gate. Three consecutive human-controlled full Windows expeditions, standalone Clear/Defeat and History/Top Runs inspection, resolution/input checks, and ordinary Alt+F4 verification remain pending. The earlier Recovery/Continue/Abandon/Quit Game interactions are documented in `Docs/TestResults/2026-09-30-WindowsGate/REPORT.md` on the earlier RecoveryFix build.

Infinite Casual remains on hold until `Docs/StandardRun_ReleaseGate_Checklist.md` passes. The Settings category rows remain the documented prototype shell except for existing implemented behavior; this follow-up does not claim that audio, graphics, or accessibility options have been completed.
