# SYNGRAVA Run Summary Implementation Report

Date: 23 September 2026  
Scope: replace the legacy end-of-run result card with a complete SYNGRAVA Run Summary, persist the statistics needed by that screen, and verify Clear, Defeat, restart, checkpoint, and responsive layout behavior.

## Result

The milestone is functionally complete. The final isolated Unity Play Mode run passed **111/111 tests**, with **0 failed** and **0 skipped**, in **65.2219161 seconds**. All six source/test hashes in `source-hashes.csv` match the exact isolated project that produced `final-results.xml`.

## Player-facing result

The legacy small `RUN COMPLETE` card is now covered by a full-screen runtime-built summary that uses the established black, violet and white SYNGRAVA visual language. The same component has two outcome states:

- **RUN CLEARED / THREAD OF FATE SECURED**, with violet accents.
- **RUN DEFEATED / THREAD SEVERED**, with a crimson outcome accent.

The screen presents:

- the ending reason and final enemy;
- resolved nodes versus total route length;
- a visual rail containing every node type actually travelled;
- final HP, Mana and Embers;
- battles won, actual non-overkill damage dealt, damage taken, healing, Critical hits, Player actions, elapsed time, total Convergences, and the most-used Convergence pattern;
- active Relics, active Anomalies and standard run modifiers;
- a friendly route string such as `BATTLE › EVENT › ELITE › REST › BOSS`, without internal identifiers such as `BATTLE_01`;
- the Run Seed for reproduction;
- one clear **BEGIN NEW JOURNEY** action that performs the existing verified New Run reset.

The former result panel remains populated behind the new overlay. This preserves existing serialized scene references and regression compatibility while the player only sees the redesigned presentation.

## UI/UX design work

The design was planned before runtime implementation in the existing SYNGRAVA Figma file:

- [Run Summary design frame](https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=9-14)
- Frame: `RUN SUMMARY / Standard Run outcome / 1660 × 930`

Unity constructs the final screen at runtime, so no scene hierarchy edit or Inspector assignment is required. Existing torn-paper, crystal, eye and node icon sprites are reused through `StandardRunMapVisualAssets`; no foreign game artwork or branding was imported.

The first render review found the title colliding with the eye ornament and the Build Archive touching the route strip. Both were corrected before final verification. The final wide and narrow captures were visually inspected and show separate panel boundaries, readable hierarchy, contained text and an accessible primary action.

## Run statistics and checkpoint behavior

`RunManager` now owns the summary counters because it already owns the active run lifecycle. The counters reset only when a New Run starts. Checkpoint capture/restore includes them, so Continue does not erase the history that the final summary needs.

The persisted values are elapsed seconds, battles won, actual damage dealt, actual damage taken, actual healing received, Critical hits, Player actions, total Convergences, and the three per-pattern Convergence counts. Validation rejects negative, non-finite, or internally inconsistent values before mutating the live run. Existing schema-3 saves deserialize missing fields as zero and remain valid.

Elapsed time uses unscaled frame time while the run is active, but stops when `Time.timeScale` is zero. This counts map and battle decision time while respecting the Settings pause.

## Integration details

- `BattleManager` records actions after all direct and Convergence damage has resolved, preventing duplicate damage accounting.
- `RunManager.ApplyDamage` and `RestoreHP` record actual applied values after HP caps, so overkill and overheal do not inflate the summary.
- Winning a battle increments the counter once behind the existing `battleEnded` guard.
- Every resolved Convergence increments its total and named-pattern count.
- Defeat clears the active checkpoint as before, then captures the final in-memory summary.
- Boss victory commits the final node, clears the checkpoint, then captures the completed route.
- Restart and map entry immediately hide the summary overlay, preventing stale presentation state.

## Automated verification

| Check | Result | Evidence |
| --- | --- | --- |
| Full Unity Play Mode regression | PASS | `final-results.xml`: 111 passed, 0 failed, 0 skipped |
| Clear summary | PASS | Existing complete-run regression now checks title, status, route copy, layout and New Journey action |
| Defeat summary | PASS | Same regression checks Defeat title/status and old retry compatibility |
| Checkpoint round trip | PASS | `TC_SUM_001` verifies every counter survives JSON capture/restore |
| Corrupt summary rejection | PASS | Inconsistent Convergence counts are rejected without partial live-state mutation |
| New Run reset | PASS | `TC_SUM_001` verifies counters reset to zero |
| Responsive bounds | PASS | Every summary text/button remains in its card at 1920×1080 and 1280×1024 |
| Text overflow | PASS | TextMesh Pro overflow assertions pass at both captured resolutions |
| Tested-source identity | PASS | Six workspace/tested SHA-256 pairs match |

The first full run exposed one genuine header-bound issue and three assumptions in older tests: two fixtures depended on a random run seed producing a particular Critical/node type, while one exact checkpoint comparison ignored that elapsed time advances between save and assertion. The header was fixed. The fixtures now calculate the deterministic Critical result, explicitly locate a Random Event, and compare checkpoint elapsed time with a bounded delta. After the visual refinement and added corrupt-checkpoint assertion, the final 111-test run passed.

## Files added

- `Assets/Project/Scripts/Battle/RunSummarySnapshot.cs`
- `Assets/Project/Scripts/Battle/RunSummarySnapshot.cs.meta`
- `Assets/Project/Scripts/UI/RunSummaryUI.cs`
- `Assets/Project/Scripts/UI/RunSummaryUI.cs.meta`

## Files updated for this milestone

- `Assets/Project/Scripts/Battle/BattleManager.cs`
- `Assets/Project/Scripts/Battle/RunManager.cs`
- `Assets/Project/Scripts/Battle/SeededRunState.cs`
- `Assets/Tests/PlayMode/BattleSnapshotTests.cs`
- `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md`

The worktree already contained unrelated modified and untracked files from earlier milestones. This task did not reset, clean, commit, or overwrite those changes.

## Evidence files

- `final-results.xml`
- `source-hashes.csv`
- `run-summary-1920x1080.png`
- `run-summary-1280x1024.png`
- `REPORT.md`

## Deliberate limits

The project does not yet have Echo Fragments, a formal ScoreCalculator, persistent run-history storage, or Local Leaderboard records. The summary therefore reports the implemented run truth instead of inventing zero-valued Score/Fragments/history entries. These should be added when their own milestones establish authoritative data. This report also does not claim a standalone Windows build, long-session telemetry, final balance approval, custom summary audio, or owner visual acceptance.

The next gameplay milestone in the agreed sequence is **Echo Fragments**. It should define the permanent currency contract first, then add the earned-fragment line to this summary without coupling permanent progression to temporary run modifiers.
