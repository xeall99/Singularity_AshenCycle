# SYNGRAVA Status Effect / Debuff / Anomaly Completion Report

Date: 23 September 2026  
Scope: complete the four-Anomaly catalog, add deterministic Critical and Speed/initiative foundations, connect real Weakness/Curse sources to Elite/Boss actions, and re-verify the entire Unity Play Mode suite.

## Result

This milestone is functionally complete and the final isolated Unity Play Mode run passed **110/110 tests**, with **0 failed** and **0 skipped**, in **67.3795565 seconds**. The tested copies of all eight changed source/test files listed in `source-hashes.csv` match the current workspace byte-for-byte.

## Status Effect and Debuff behavior

`BattleStatusController` remains the single owner of temporary battle status state. Statuses reset when a new battle begins and are not serialized as run modifiers.

| Status | Source now available in gameplay | Runtime rule |
| --- | --- | --- |
| Burn | Ashen Fang relic after a successful Basic Attack | Deals end-of-enemy-turn damage equal to stacks, maximum 3 stacks, starts/refreshes at 2 turns |
| Weakness | Elite action **Ashen Crush** | Target outgoing damage is multiplied by 0.75 for 2 turns |
| Curse | Boss action **Abyssal Rupture** | Target incoming damage is multiplied by 1.20 for 2 turns |

Elite and Boss status effects are included in `EnemyActionPlan`. Analyze can therefore reveal the status together with the prepared action. The status is applied after the source attack lands and the Player survives, so it affects subsequent actions rather than retroactively changing the source hit. Existing Guard, phase and intent behavior is preserved.

## Completed four-Anomaly catalog

| Anomaly | Benefit | Consequence | Implemented details |
| --- | --- | --- | --- |
| Collapsed Star | Skill damage +30% | Skill Mana cost +1 | Actual cost is checked before Mana mutation; bonus feeds the existing Convergence damage pipeline |
| Black Sun | First Convergence in each battle resolves after 2 actions | Effective Max HP -15 | Two-action result uses the existing named-pattern resolver where possible; bonus is consumed once per battle |
| Frozen Orbit | Guard protects against 2 damaging enemy attacks | Player Speed -2 | Base Speed 10 becomes 8; enemy baseline Speed 9 therefore opens every round while this Anomaly is active |
| Inverted Pulse | Successful Basic Attack restores 2 HP | Basic Attack cannot Critical | Healing is capped by Effective Max HP; Skill attacks retain normal Critical eligibility |

All four Anomalies are unique active-run modifiers. The seeded Elite reward offer returns up to three distinct unowned choices. A selected Anomaly uses the existing modifier checkpoint payload, survives Continue, cannot stack with itself and is removed on New Run. The enum additions were appended so existing numeric values remain stable; no checkpoint schema bump was required.

## Critical and Speed foundations

`BattleTurnRules` centralizes the new deterministic combat rules:

- Player Base Speed: 10.
- Temporary enemy baseline Speed: 9.
- Equal Speed favors the Player.
- Frozen Orbit reduces Effective Speed by 2, with a minimum of 1.
- Base Critical chance: 5%.
- Critical damage multiplier: 1.5, rounded once after the existing status damage pipeline.
- The Critical roll is derived from run seed, route depth and per-battle strike index. Replaying the same run and action sequence produces the same result.
- Inverted Pulse disables the Critical roll only for Basic Attack. Skills can still Critical.

The current combat model is one Player against one Enemy. Enemy Speed is intentionally a code-level baseline because the present `EnemyData`/SQLite schema has no Speed column. A future multi-enemy initiative milestone should move enemy Speed into content data and replace this comparison with a proper initiative queue.

## Turn-flow changes

- Battle start and each new round compare Player and enemy Speed.
- When the enemy wins initiative, its prepared action resolves before Player input becomes available.
- After the Player acts, the enemy does not act twice if it already opened that round.
- Frozen Orbit Guard tracks two damaging hits. A zero-damage charge action does not consume a protected hit.
- Strike index and initiative state reset per battle, preserving deterministic replay boundaries.

## Files added

- `Assets/Project/Scripts/Battle/BattleTurnRules.cs`
- `Assets/Project/Scripts/Battle/BattleTurnRules.cs.meta`

The earlier stage of this same milestone also added:

- `Assets/Project/Scripts/Battle/BattleStatusController.cs`
- `Assets/Project/Scripts/Battle/BattleStatusController.cs.meta`
- `Assets/Project/Scripts/Battle/RunAnomalyCatalog.cs`
- `Assets/Project/Scripts/Battle/RunAnomalyCatalog.cs.meta`

## Files updated in the completion stage

- `Assets/Project/Scripts/Battle/BattleManager.cs`
- `Assets/Project/Scripts/Battle/EnemyActionPlan.cs`
- `Assets/Project/Scripts/Battle/RewardDefinition.cs`
- `Assets/Project/Scripts/Battle/RunAnomalyCatalog.cs`
- `Assets/Project/Scripts/Battle/RunStatController.cs`
- `Assets/Tests/PlayMode/BattleSnapshotTests.cs`
- `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md`

The worktree already contains unrelated modified/untracked files from earlier milestones. This task did not reset, clean, commit or overwrite those changes.

## Automated verification

| Check | Result | Evidence |
| --- | --- | --- |
| Solution compilation | PASS | `dotnet restore` and `dotnet build --no-restore`: 0 errors; 4 existing obsolete-API warnings |
| Full Unity Play Mode regression | PASS | `final-results.xml`: 110 passed, 0 failed, 0 skipped, 67.3795565 seconds |
| Tested-source identity | PASS | Eight local/tested SHA-256 pairs match; mismatch count 0 |
| Duplicate/symbol review | PASS | New Anomaly, initiative, status-plan and test symbols resolve to their intended definitions/usages |

The four warnings are existing API migration warnings in `MainMenuUI` and the existing `FindFirstObjectByType` usage in `BattleManager`; they do not indicate an error in this milestone.

## New Play Mode coverage

- `TC_STATUS_003_EliteAndBossPlansApplyVisibleDebuffs`: Elite Ashen Crush applies Weakness and Boss Abyssal Rupture applies Curse with the expected duration and visible plan description.
- `TC_ANO_004_FrozenOrbitChangesInitiativeAndGuardsTwoAttacks`: Effective Speed becomes 8, the enemy opens the round, and one Guard protects against exactly two damaging attacks.
- `TC_ANO_005_InvertedPulseHealsButBasicAttackCannotCritical`: a seed/index that would Critical without the Anomaly is used to prove Basic Attack remains non-critical while restoring exactly 2 HP.
- Existing catalog tests now require four supported Anomalies and three distinct seeded choices.
- Existing relic, branch and battle regression fixtures were isolated from the new initiative/Critical variables where those mechanics were outside the test's purpose.

## Regression history and fixes

During development, one Frozen Orbit test initially assumed a fixed 10-damage enemy action although the database action dealt 12. The assertion was corrected to read the prepared action's actual damage. A subsequent run exposed that the reward application switch did not yet accept the two appended Anomaly effects; production code was corrected. Two legacy tests also relied on implicit assumptions that the Player always starts and attacks never Critical; those fixtures now explicitly select non-Frozen choices or calculate their expected deterministic damage. The final run occurred after all production and test corrections.

## Evidence files

- `final-results.xml`: final Unity Test Framework result, 110/110 passed.
- `source-hashes.csv`: SHA-256 comparison between the active workspace and the isolated Unity project that produced the final result.
- `REPORT.md`: this implementation and verification record.

## Remaining limits

This report does not claim a standalone Windows build, final player-facing balance approval or long-session telemetry. The Speed 10/9 comparison, Critical chance 5%, Critical multiplier 1.5, Frozen Orbit values and Inverted Pulse heal are current design values and should be revisited after playtesting.

The requested Status Effect/Debuff/Anomaly feature set is complete at its current GDD scope. The recommended next gameplay milestone is **Run Summary**: show the completed route, ending reason, active relics/anomalies, key battle statistics, and final resources without changing the verified run-state rules.
