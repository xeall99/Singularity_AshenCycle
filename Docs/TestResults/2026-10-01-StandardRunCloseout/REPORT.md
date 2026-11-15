# Standard Run closeout and Named Convergence score correction

Session: 1–2 October 2026. Work resumed from the existing isolated Windows expedition; no combat skip, injected battle result, or debug victory was used for the standalone route below. Computer Use controlled ordinary game input. Art remains deferred at the owner's request.

## Decision and evidence levels

**Technical closeout PASS; formal release acceptance remains pending for the exact post-fix executable.** The Windows save/Continue/Recovery/Clear/Abandon checks below passed on the AnomalyGlyph build. The playthrough exposed a scoring defect: model 1 awarded the Named Convergence bonus to Unstable Pulse. The correction now passes 144/144 regression cases, a fresh Windows build, and focused standalone checks of legacy compatibility, both pattern categories and Continue. Evidence from the earlier full expedition remains attached to its original executable; the post-fix smoke is not another full expedition or human acceptance session.

The owner previously reported items 1, 2, 3, 7 and 8 of the release checklist as completed: three human-controlled runs, Continue, Defeat, resolution/input, and ordinary exit. These are **USER-REPORTED PASS**. Individual seeds, RunIds, dates and executable identity were not supplied. They are not relabelled as tool-observed sessions or three newly performed runs. Items 4–6 were explicitly left for technical checking and are covered by this session's Clear, Retry Save, Recovery and Abandon evidence.

## Tested executable and isolated data

- Unity 6000.6.0f1; Windows x86_64 Development; interactive resolution 1920×1080.
- Pre-fix executable: `Builds/Verification-2026-09-30-AnomalyGlyph/SYNGRAVA.exe`.
- Executable SHA-256: `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`.
- Company/product: `CodexGateTests/SYNGRAVA_AnomalyGlyphGate_20260930`.
- Persistent directory: `%USERPROFILE%/AppData/LocalLow/CodexGateTests/SYNGRAVA_AnomalyGlyphGate_20260930`; save files are under its `Saves` subdirectory.
- Before the score fix, 52/52 recorded source/config/test/build-script hashes matched the previously tested project, including the resumed 2 October comparison. This is a defined manifest comparison, not a claim about every repository file.
- The real player's profile was not used. Controlled write failure and corruption affected only this test profile, after its checkpoint/backup were copied into this evidence directory.

## Save transactions and terminal result

| Check | Result | Evidence |
| --- | --- | --- |
| Reward checkpoint fails visibly | PA SS | Locking only the isolated `.tmp` file produced `PROGRESS NOT SAVED` and `RETRY SAVE` on the committed map boundary. `save-failure-retry-Player.log`. |
| Retry still fails safely | PASS | Warning remained; the original checkpoint hash stayed `A8FEE701655D3A1ABB6EC1A9D9A72B4E6A1E378916AF1C3EC2359D0A8B9BC38B`. `failed-retry-hash-check.json`. |
| Retry succeeds after access restored | PASS | Lock released; warning disappeared and depth-2 checkpoint persisted HP 103, Mana 2, Embers 10 and Vital Core +15. `successful-retry-checkpoint.json`. |
| Continue after reward and relaunch | PASS | Restored `StateJson` exactly equals the successful checkpoint. `continue-post-reward-check.json`, `continued-post-reward-checkpoint.json`. |
| Recovery of truncated primary | PASS | Closed game, retained valid depth-2 backup, truncated only the test primary, then selected Recover in the game. Recovered `StateJson` exactly matches the backup, including identity and temporary modifier. `recovery-check.json`, `recovery-primary-before.json`, `recovery-backup-before.json`, `recovered-primary.json`. |
| Natural full Standard expedition | PASS for route/combat completion | 10/10 nodes and Abyss Herald defeated with ordinary commands. `pre-boss-checkpoint.json`, `clear-Player.log`, `clear-history.json`, `clear-wallet.json`. Scoring discrepancy is separately documented below. |
| Clear remains terminal after relaunch | PASS | Continue shows NO SAVE; primary and backup absent. One History record, one Echo claim, wallet 138, payloads unchanged. `clear-relaunch-check.json`, `clear-close-Player.log`. |
| History and Top Runs presentation | PASS on model 1 build | Both panels visibly show one Clear, 10/10, score 2,950 and matching route/resources. This is the original observed score, not a model-2 result. |
| New Expedition after Clear | PASS | New seed/RunId, depth 1, HP 100, Mana 5, Embers 0; empty modifiers/visited nodes and zero combat statistics. `post-clear-new-run-checkpoint.json`. |
| Cancel Abandon | PASS | Started another natural battle, selected Vital Core, relaunched, opened New Expedition and chose Cancel. Checkpoint SHA-256 unchanged. `pre-abandon-checkpoint.json`, `abandon-cancel-check.json`. |
| Confirm Abandon | PASS | RunId changes from `cedf2fae2b75402cad5da00ca78a9396` to `fd3e623b77b74d4f8528ff3902ffd916`; depth 1, HP 100, Mana 5, Embers 0, no modifiers/visited nodes/actions. `post-abandon-checkpoint.json`, `abandon-confirm-check.json`, `abandon-Player.log`. |
| Ordinary close | PASS in this tool session | Alt+F4 after committed boundaries and Summary closed the player; relaunches succeeded. Historical native-crash cause on an older build remains undetermined. |

The lock helper is restricted to the exact AnomalyGlyph test profile and has a 15-minute timeout. Its handle was released. The deliberate write failures are expected test faults; they are not represented as an exception-free session. Unity development-player shutdown still records existing graphics/ComputeBuffer cleanup warnings. No screenshots were exported into this folder; visual observations were displayed in the Computer Use session.

## Full expedition

Run seed `-2134465660`; RunId `f10f2b2ba75a42f1bd60e3d9dc030d26`; route version 1; checkpoint schema 3; ruleset 0.4. The run resumed from its existing stage-1 checkpoint. Recovery rolled back the Ashen Cache transaction to the saved depth-2 backup and the event was played again before continuing.

| Node | Encounter / chosen result |
| --- | --- |
| 1 | Ashen Wretch; two Skills; Vital Core I (+15 Max HP). |
| 2 | Ashen Cache; Take Embers. |
| 3 | Veil Knight; Guard, Skill, Skill, Attack; Vital Core I. |
| 4 | Veil Knight; Attack, Skill, Attack, Attack; Vital Core I. |
| 5 | Silent Well; Mend Wounds, paying 3 Mana. |
| 6 | Veil Knight; Skill, Skill, Attack; Vital Core II (+23 Max HP). |
| 7 | Elite Veil Knight; Skill, Attack, Skill; Inverted Pulse. |
| 8 | Rest; recovered 55 HP up to 139/168. |
| 9 | Veil Knight; Attack, Attack, Skill, Attack; Event Horizon adds 6 damage, Inverted Pulse heals 2 HP per Attack; Attack Surge II (+15%). |
| 10 | Abyss Herald; Attack, Attack, Skill, Attack; Event Horizon adds 6 damage and the boss is defeated. |

Observed Summary: HP 66/168, Mana 3/10, Embers 140, Echo +138 and total 138; 7 battles won, 350 damage dealt, 235 damage taken, 164 healing, 6 total Convergences, 2 critical hits and 24 actions. Relics NONE; Anomaly Inverted Pulse; modifiers Attack +15% / Max HP +68. Displayed active duration 16:48 includes verification interruptions and is not a balance benchmark.

Model-1 breakdown was 500 Battle +150 Event +500 Elite +1,500 Boss +300 Convergence = **2,950**. Four Unstable Pulses were incorrectly score-eligible; only the two Event Horizons qualify under the approved design. The same route/pattern counts would yield **2,750** under model 2. This is an expected calculation, not a rewritten historical result. Echo reward does not change.

## Focused correction

- `RunManager.RecordConvergence` counts only Event Horizon/Reversal Orbit toward the three-bonus limit. Fallback statistics, Orbit behavior and the mana effect remain intact.
- `ScoreCalculator.ModelVersion` is 2. Existing History supports earlier positive model versions, while the existing leaderboard filter selects the current version only. Old score records and Echo claims are not modified.
- `SeededRunState` adds optional `ScoreModelVersion` within schema 3. Current counters round-trip exactly. On legacy restore, progress/resources/modifiers and total pattern statistics survive, but old capped bonus counters start at zero because a per-battle Named/fallback split is unavailable. Committed node points remain available; future Named patterns score normally. No guessed retroactive bonus is granted.
- Validation rejects unknown future score models and current-model eligible counters exceeding the total Named pattern count before mutating runtime state.
- Three regression cases cover fallback/cap interaction and round-trip, legacy checkpoint compatibility/future rejection, and readable old History excluded from current ranking. Existing fixture assertions use the current model rather than hardcoding 1.

Production changes are limited to `RunManager.cs`, `ScoreCalculator.cs`, `SeededRunState.cs` and `BattleSnapshotTests.cs`; `score-fix-changes.csv` records those four changed paths against the pre-fix manifest. No Inspector assignment, scene/prefab change, package edit, SQLite migration, art replacement or commit is required. Existing unrelated workspace changes are preserved. GDD design is unchanged: this restores the already documented Named-only rule.

## Post-fix verification

Complete regression: **144/144 PASS**, 0 failed/skipped, 145.5732346 seconds. All six `TC_SCORE_*` cases pass. Evidence: `full-results.xml`, `playmode-summary.json`, `unity-playmode.log`. `source-hashes-after-score-fix.csv` confirms 52/52 workspace/test-project paths match before the run. The sandboxed launch could not connect to the local Licensing Client; one retry collided with that still-running test process. Only that owned isolated process was stopped after checking its command line. Those launch attempts do not count as gameplay test failures or successful verification.

The first build reused the test project's Library. BuildPipeline reported 0 errors / 514 warnings, but standalone startup logged 16 missing-script warnings on MainMenuCanvas and seven buttons. Runtime bootstrap displayed the menu; this did **not** make that candidate acceptable. Its player/build logs and hashes are retained as `first-build-missing-bindings-*` and `first-build-hashes.json`. Project `.meta` files matched the reused copy, and source/scene hashes matched; a stale imported-script cache was suspected, not proven at this stage.

The final candidate was rebuilt successfully with the repository's `Tools/Build-WindowsStandalone.ps1`, which copied Assets/Packages/ProjectSettings into a fresh isolated directory and used already-resolved package sources. `source-hashes-fresh-build.csv` confirms 52/52 matching inputs. The production project and its `.meta` files were not edited for this rebuild. The fresh copy was removed by the build tool after success.

- Final executable: `Builds/Verification-2026-10-02-NamedScore/SYNGRAVA.exe`; Windows x86_64 Development, Unity 6000.6.0f1.
- Identity: `CodexGateTests/SYNGRAVA_NamedScoreGate_20261002`; independent test saves under that profile's `Saves` folder.
- Build: **0 errors, 514 warnings**, 1,091.26 seconds, 332,422,990 bytes. Evidence: `build-summary.json` and the candidate's `unity-build.log`. The build warnings are not represented as zero warnings.
- Executable SHA-256: `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`. Unity's executable bootstrap is identical across these builds, so executable hash alone does not identify the gameplay assembly.
- Final `SYNGRAVA_Data/Managed/Assembly-CSharp.dll` SHA-256: `287ACD680FF796B40594E98A5DD95CF428A03329EB917C8129F26B359A1CE076`. `build-hashes.json` identifies both files; first-attempt hashes are retained separately.
- Fresh startup, battle/reward smoke and Continue/relaunch logs each record **0 missing-script messages and 0 exception/crash messages**. Evidence: `fresh-build-startup-check.json`, `fresh-build-startup-Player.log`, `score-build-smoke-Player.log`, `score-build-continue-Player.log`, `score-build-smoke-check.json`. The fresh import resolved the observed missing-script symptom; a stale cache remains an inference, not a proven root-cause diagnosis.

## Focused standalone checks on the final candidate

The test profile received only copies of this session's legacy stage-1 checkpoint, model-1 History and Echo wallet. Existing destination save files were checked before copying. Real player profiles were untouched. Seed `-1957354374`, RunId `fd3e623b77b74d4f8528ff3902ffd916` remained unchanged throughout the focused run. Input was ordinary game input at 1920×1080.

| Check | Result and observed state | Evidence |
| --- | --- | --- |
| Legacy checkpoint Continue | PASS; Main Menu offered Stage 1/10 and Continue opened its map. Legacy identity, HP 100, Mana 5, Embers 0 and empty temporary build remained in the checkpoint. Loading alone did not rewrite the save. | `score-build-legacy-continued-checkpoint.json` |
| Old History / new leaderboard | PASS; History displayed the original Clear score 2,950 with MODEL 1; Top Runs showed 0 ranked runs. Echo balance remained 138. | Computer Use observations; payload comparisons in `score-build-smoke-check.json` |
| Fallback effect without score | PASS; four Basic Attacks naturally defeated Ashen Wretch. The third triggered Unstable Pulse and restored 1 Mana. Vital Core I committed Stage 2, HP 79/115, Mana 9, Embers 10, model 2, one Unstable Pulse and **0 eligible bonuses**. | `score-build-fallback-checkpoint.json` |
| Named effect and committed bonus | PASS; selected connected `NormalBattle_02_L0`, then Attack, Attack, Skill, Attack defeated Veil Knight. Event Horizon visibly added 6 damage. A second Vital Core I committed Stage 3, HP 52/130, Mana 9, Embers 28, model 2, one Event Horizon, one Unstable Pulse and **1 eligible bonus**. | `score-build-named-checkpoint.json` |
| Relaunch / Continue | PASS; Alt+F4 closed the player, relaunch offered Stage 3/10, and Continue returned to the same map. Checkpoint `StateJson` remained byte-for-byte identical, including model/counters, route, resources and modifiers. Current-model runtime counter restoration is also covered by `TC_SCORE_004`. | `score-build-continued-checkpoint.json`, `score-build-smoke-check.json`, `score-build-continue-Player.log` |
| Archive and wallet preservation | PASS; original History and Echo wallet payloads remained identical, with no new terminal claim during this unfinished smoke expedition. | `score-build-smoke-check.json` |

The owned player was closed after verification; the valid Stage-3 test checkpoint is retained. No run was injected, no debugging victory was used, and no score-2 full Clear is claimed from this short smoke. The full 144-case suite and the earlier standalone transaction/full-route evidence provide the remaining technical coverage.

## Remaining release work

The technical checks requested for Clear, Retry Save, Recovery and Abandon are complete. Previously supplied human completion reports remain accepted as USER-REPORTED PASS. Formal acceptance of the post-fix candidate still needs to be associated with this executable and its gameplay assembly; seeds/dates/build metadata for the owner's earlier sessions are still unavailable. Do not invent those records or mark the exact-build human gate complete from automated/tool checks. `source-hashes-final.csv` confirms the workspace still matches all 52 recorded final build inputs; scoped `git diff --check` passed.

Settings categories remain a prototype/shell; tutorial, a complete SQLite CRUD demonstration and final player/enemy art remain separate product work. Infinite Casual waits for the Standard Run release gate. Existing development-player graphics cleanup warnings and the 514 build warnings remain recorded limitations, even though no crash or missing script was observed on the final candidate. Repository whitespace checks and final manifest consistency checks accompany this closeout; no commit or push was performed.
