# Guided First-Run Tutorial — 14 November 2026

## Goal and design

The owner asked for first-time guidance that demonstrates play directly: show the next task, spotlight the actual control to use, advance after the player's action, and offer a way to skip. The existing six-page Field Guide remains available as a replayable reference.

The design follows the owner’s written description of the linked video. The supplied YouTube page could not be retrieved by the available research tool, so this report does not claim to have inspected its scenes. Research and the full flow/acceptance plan are recorded in [`Tutorial_DesignPlan.md`](../../Tutorial_DesignPlan.md). Relevant references include [Apple Developer’s game onboarding guidance](https://developer.apple.com/app-store/onboarding-for-games/) and [Roblox Creator Hub’s onboarding techniques](https://create.roblox.com/docs/production/game-design/onboarding-techniques).

## Implementation

- Added `Assets/Project/Scripts/UI/GuidedTutorialController.cs` as a separate runtime tutorial on the existing Battle Canvas. It does not require an Inspector assignment or a scene/prefab edit.
- Connected its flow to the actual Standard Run map and real combat controls through `BattleManager`: reachable route node, Enter Node, Basic Attack, Shadow Strike, Guard, and Analyze. The spotlight hole routes input to the real target; four surrounding dim panels block other gameplay controls.
- Kept a visible **SKIP TUTORIAL** option in each phase and Escape as an equivalent shortcut. Completion or skip is recorded by `UserSettingsStore` using `guided_tutorial_completed`; no database schema or save format changed.
- Waits for actual combat turns and action resolution. An enemy-first turn is allowed to resolve normally before directing the player. Insufficient-Mana Skill and Analyze actions are explained without trying a disabled button or consuming a turn. An early victory closes guidance and leaves the regular reward flow intact.
- The separate replayable Field Guide remains on the Main Menu. The behavior and implementation boundary are recorded in [`PROJECT_CONTEXT.md`](../../SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md).

## Automated coverage

Five guided-tutorial tests were added to `BattleSnapshotTests`:

| Test | Coverage |
| --- | --- |
| `TC_TUT_001_FirstRunGuidesRealMapAndCombatActions` | First-run map and combat guidance; actual target controls; blocks a non-target map control; checks panel/text containment; Attack, Skill, Guard/Convergence, enabled Analyze, and completion persistence. |
| `TC_TUT_002_SkipAndEscapePersistWithoutChangingRunProgress` | Skip on the map and Escape during battle preserve progress and persist completion. |
| `TC_TUT_003_EarlyVictoryClosesGuidanceAndKeepsAutomaticReward` | Early battle victory closes guidance without interrupting the normal reward. |
| `TC_TUT_004_LowManaExplainsDisabledSkillAndAnalyzeWithoutSpendingTurn` | Disabled Skill and Analyze paths explain the requirement; informational Continue does not consume Mana, a turn, or an Orbit action. |
| `TC_TUT_005_EnemyFirstAnomalyWaitsForPlayerAndShowsRealOpening` | An enemy-first anomaly resolves its actual opening before Basic Attack is highlighted. |
| `TC_TUT_006_DefeatDoesNotCompleteGuidedTutorial` | Defeat ends the current guidance without marking it complete, keeping it eligible on a future fresh run. |

`TC_ANO_002_CollapsedStarAppliesBenefitAndConsequenceTogether` and `TC_RELIC_006_BurnTicksExpiresCapsAndClearsBetweenBattles` calculate expected critical damage from the active run seed. This avoids assuming that a seeded attack cannot critically hit.

## Verification

Unity Editor `6000.6.0f1`, full `BattleSnapshotTests` Play Mode suite, each execution on an isolated test project/profile. The pre-implementation baseline ran before the guided tutorial code was added. The three post-implementation runs were consecutive; all results are clean, with 0 failed and 0 skipped tests.

| Run | Result | Passed | Failed | Skipped | Duration |
| --- | --- | ---: | ---: | ---: | ---: |
| Pre-implementation baseline | Passed | 152 | 0 | 0 | 136.471 s |
| Post-implementation 1 | Passed | 158 | 0 | 0 | 135.039 s |
| Post-implementation 2 | Passed | 158 | 0 | 0 | 131.485 s |
| Post-implementation 3 | Passed | 158 | 0 | 0 | 139.052 s |

The archived XML and full Unity logs are:

- `baseline-results.xml`, `baseline-unity.log`
- `guided-playmode-pass-1.xml`, `guided-playmode-pass-1.log`
- `guided-playmode-pass-2.xml`, `guided-playmode-pass-2.log`
- `guided-playmode-pass-3.xml`, `guided-playmode-pass-3.log`

Two focused regressions also passed 1/1 each: `TC_TUT_006_DefeatDoesNotCompleteGuidedTutorial` (`guided-defeat-focused.xml`/`.log`) and the seed-aware `TC_RELIC_006` (`relic-seed-aware-focused.xml`/`.log`).

An earlier, non-counted 158-test sweep passed once, then failed `TC_RELIC_006` because that test expected a non-critical damage value (189 HP) while the active run seed produced a critical hit (184 HP). The assertion was made seed-aware and its focused test passed before starting the final three-run streak. That preliminary pass and failure are preserved as `preliminary-before-stability-pass-1.*` and `preliminary-seed-sensitive-failure.*`; neither is included in the final consecutive results.

## Scope and limitations

Automated Play Mode checks confirm the modeled flow, real-control hookups, input blocking, and turn/resource outcomes. The tests also check that the first-run panel/text fit the current Canvas. No manual game session, screenshot-based review at multiple window sizes, standalone build, or human learning evaluation was performed. The suite cannot establish that new players learn more effectively; that requires a later visual and player acceptance review.
