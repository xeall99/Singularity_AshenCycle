# SYNGRAVA — Guided First-Run Tutorial Design

**Revision:** 2 — replaces the passive-only onboarding interpretation from 14 November 2026.  
**Status:** Design and acceptance plan completed before implementation; implementation authorized by the owner.  
**Scope:** A one-time guided first-run flow in the existing Standard Run, plus the existing replayable Field Guide.

## Player need

The owner wants a new player to be shown what to do in the real game: a short instruction appears, the relevant actual button/node is highlighted, the player performs that action, and only then does the guide move to the next step. The guide must offer **SKIP TUTORIAL** throughout.

The earlier six-page Field Guide is useful as a reference but is not a substitute for this guided onboarding. Keep it available from the Main Menu after this change.

The supplied YouTube URL could not be fetched by the available web research tool. This design therefore follows the owner's written description of the example rather than making claims about the video itself.

## Research and design decisions

- Apple recommends that game onboarding stay fast, place instruction beside the relevant interface area, and provide a way to review controls later. [Onboarding for Games — Apple Developer](https://developer.apple.com/app-store/onboarding-for-games/)
- Roblox Creator Hub describes fixed-sequence and contextual onboarding, recommends teaching the core loop, and gives highlighting the relevant button as an example of a timely hint. [Onboarding techniques — Roblox Creator Hub](https://create.roblox.com/docs/production/game-design/onboarding-techniques), [Onboarding — Roblox Creator Hub](https://create.roblox.com/docs/production/game-design/onboarding)
- The tutorial must teach only actions available in the current build. It must not grant resources, alter enemy behavior, add a tutorial-only battle, change the reward/run rules, or pretend a disabled action can be used.
- Use the existing uGUI/Canvas, TextMeshPro, Unity `Button`, `StandardRunMapUI`, `BattleManager`, and `UserSettingsStore` patterns. No UI framework/package migration.

## Trigger and persistence

1. On a new installation/profile, automatically offer the guided tutorial when an untouched Stage 1 map is first shown: Standard Run is active, depth is 1, no node has been visited, and the setting `guided_tutorial_completed` is not `true`.
2. The same trigger applies to a Continue that restores that untouched Stage 1 map. Do not interrupt a run that has already entered a node.
3. Save completion in the existing preferences database through `UserSettingsStore`; do not change the SQLite schema. Write the completion marker only after the tutorial finishes or the player skips it. A storage error must not block play; log it and allow a later retry.
4. **SKIP TUTORIAL** is visible in every tutorial phase. Escape also skips. Skipping hides guidance immediately, preserves current run state, allows normal play, and records completion so the game does not auto-open the tutorial next time.
5. Keep **FIELD GUIDE** on the Main Menu as an optional, replayable reference. Opening the Field Guide does not mark the guided tutorial complete.

## Guided flow

The flow uses the player's actual run and its real controls. Instructions remain concise and contextual; the tutorial does not automate button presses.

| Step | Situation | Instruction and target | Player action / transition |
|---|---|---|---|
| 1 | Untouched Stage 1 map | Explain that a connected node begins the journey. If the map has multiple reachable choices and none is selected, spotlight one reachable node and ask the player to select it. If the map has already selected its sole reachable node, move straight to the next step. | Select the highlighted reachable node when a choice is needed. |
| 2 | A node is selected | Explain that this choice enters the encounter; spotlight **ENTER NODE**. | Activate the real Enter Node button. |
| 3 | First battle is ready | Briefly explain player/enemy turns and that the enemy's action is prepared; explain the opening action if the enemy has initiative. Spotlight **BASIC ATTACK** when the player can act. | Make a real Basic Attack. If the enemy acts first, allow its real opening action to resolve visibly before spotlighting the player's command. |
| 4 | Player turn after the first response | Explain Mana cost and damage; spotlight **SHADOW STRIKE** only while the real button is interactable. | Use the real Skill. |
| 5 | Player turn after the Skill response | Explain that **GUARD** reduces the incoming hit, restores Mana, and fills an Orbit slot; spotlight **GUARD**. With Attack + Skill already recorded, Guard demonstrates the third-action Convergence. | Use the real Guard. |
| 6 | Player turn after Guard response | Explain that **ANALYZE** reveals the already-prepared enemy action, costs 75% of effective Max Mana rounded up, is once per node, does not spend the player's turn, and does not enter Orbit. Spotlight the real Analyze button if enabled. | Use Analyze when it is interactable; otherwise show the same tip and a **CONTINUE** action (never force a disabled click). |
| 7 | After Analyze or the non-interactable Analyze explanation | Confirm that the player can now choose any available combat action; show **CONTINUE** and **SKIP TUTORIAL**. | Close the overlay without consuming a turn; persist completion and leave the current player turn intact. |

If a battle is won before all combat steps are reached, end the tutorial cleanly at the result/reward screen and allow the normal reward flow to continue. Do not hold a victory, defeat, or reward transition open waiting for a tutorial action. If defeated before completion, do not claim the tutorial completed; on a future untouched new run the player can see it again. If the player skips, that choice is final for automatic onboarding.

## Highlight and input behavior

- Build a dim full-screen layer with a cutout around the current real target, a high-contrast violet/white animated outline, and a compact instruction panel placed near the target where screen space permits.
- The actual highlighted `Button` remains the click/focus/submit target. Four raycast-blocking dim regions surround its bounds so unrelated game controls cannot be clicked through the tutorial. The tutorial card and Skip/Continue actions remain clickable. Do not add a fake button that only looks like the game's real control.
- For map selection, the current reachable node is the first target; after the player selects a valid node, move the spotlight to that node's real **ENTER NODE** control. Keep the map interactive only through the spotlight.
- After a combat action is activated, remove the dim layer while the real action/enemy turn/animation resolves. Reopen it on the next player turn for the next instruction. Analyze is turn-free, so its completion instruction remains on the same turn.
- Do not set `Time.timeScale` to zero. Do not suppress or rewrite combat rules. Wait for the existing action flow to become ready before presenting the next target.
- Support mouse and EventSystem keyboard submit/focus. Escape skips. Keep the Skip button visible and reachable throughout.
- Resize and reposition the spotlight/prompt from the target's RectTransform so it follows the map scroll and stays inside the Canvas at supported window sizes.

## Implementation boundaries

- Keep `TutorialUI` as the replayable six-page Field Guide; add a separate `GuidedTutorialController` for the first-run overlay.
- Attach/build the controller at runtime on the existing Battle Canvas; no scene/prefab or Inspector assignments are expected.
- Connect the controller to existing real map/action controls and successful action/turn state transitions. Failed/disabled actions do not advance the lesson.
- Persist one completion preference with `UserSettingsStore`; no schema, package, base stats, combat balance, enemy plan, route, reward, checkpoint, history, or wallet changes.
- Update `BattleSnapshotTests` with deterministic tests using the isolated test profile. Keep unrelated player changes and current serialized scene references.

## Acceptance criteria

1. A fresh profile automatically starts onboarding on the untouched Stage 1 map; an already-entered run does not get interrupted.
2. The reachable node, **ENTER NODE**, **BASIC ATTACK**, **SHADOW STRIKE**, **GUARD**, and the enabled **ANALYZE** target are spotlighted in the order above. Each step advances only after the corresponding real action or explicit Continue.
3. Non-target controls are blocked during a target step; the actual highlighted control works through mouse and EventSystem submit/focus.
4. Combat and enemy-turn visuals remain visible; tutorial transitions wait for real game-turn readiness. Analyze does not consume the current turn or enter Orbit.
5. If an action ends the encounter early, the victory/reward flow is not blocked. Defeat is not falsely recorded as tutorial completion.
6. Skip works from map, battle, and informational steps; Escape has the same behavior. Skip does not grant/lose in-game resources or alter run progress.
7. Completing or skipping persists through the existing settings store. A later new run does not auto-open the guided tutorial. Field Guide can still be opened from Main Menu and does not change this preference.
8. No timeScale, HP, Mana, enemy state, Orbit, reward, checkpoint, Echo, or History data changes except through the real player actions and normal game flow.
9. The panel, spotlight, real target, and text remain inside the Canvas at 1920×1080 and 1280×720/1024 with no text overlap/overflow.
10. Run the full automated Play Mode baseline once before implementation, then the full Play Mode suite three consecutive times after implementation on isolated profiles with distinct XML/log files. A failed run breaks the streak.

## Verification plan

- **Pre-implementation baseline:** Run `BattleSnapshotTests` once on an isolated Unity project copy matching the current source; record total, failures/skips, duration, XML, and log before changing feature code.
- **Focused tests:** New profile auto-trigger; Stage 1-only guard; route target selection then Enter Node; actual Attack → Skill → Guard Orbit completion; enabled and insufficient-Mana Analyze branches; each step's real target/cutout/input gating; enemy-first opening and action-resolution transitions; early victory/defeat; skip/escape persistence; Field Guide independence; save/run resource preservation; and two-resolution containment.
- **Regression:** Run the entire `BattleSnapshotTests` Play Mode suite three consecutive times after all fixes. Store separate result XML and Unity logs in the milestone report folder; a blocked attempt is not a pass.
- **Static review:** Inspect changed files, check no new scene/prefab/settings schema, search for missing assets/types/references, and verify that unrelated existing changes remain untouched.
- The prior Field Guide's 152/152 tests are historical evidence only; they do not validate this guided tutorial revision.

## Implementation status — 14 November 2026

Implemented as a separate `GuidedTutorialController` on the existing Battle Canvas. `TutorialUI` remains the replayable Main Menu Field Guide. The guided flow uses the real map node, Enter Node, Basic Attack, Shadow Strike, Guard, and Analyze controls. The real target stays clickable while the surrounding dim panels block unrelated controls. The prompt and Skip control remain visible; Escape also skips. The overlay waits for real combat turns and does not pause time or grant resources. Completion/skip is stored through `UserSettingsStore` under `guided_tutorial_completed`.

The tutorial adapts when an action is unavailable: it explains an insufficient-Mana skill and guides the player through Guard/recovery before continuing; unavailable Analyze is explained with a non-turn-consuming Continue step. If the enemy acts first, the player can watch its actual opening before the tutorial targets Basic Attack. A battle clear closes guidance without holding up the existing reward flow. The Field Guide remains separately accessible from the Main Menu.

Automated acceptance coverage added to `BattleSnapshotTests`:

| Test | Verified behavior |
| --- | --- |
| `TC_TUT_001_FirstRunGuidesRealMapAndCombatActions` | Fresh first-run map guidance and progression through actual map/battle controls. |
| `TC_TUT_002_SkipAndEscapePersistWithoutChangingRunProgress` | Skip and Escape close onboarding and persist completion without changing run progress. |
| `TC_TUT_003_EarlyVictoryClosesGuidanceAndKeepsAutomaticReward` | Early clear closes the tutorial and preserves the normal reward flow. |
| `TC_TUT_004_LowManaExplainsDisabledSkillAndAnalyzeWithoutSpendingTurn` | Disabled Skill/Analyze explanations do not consume an extra action or turn. |
| `TC_TUT_005_EnemyFirstAnomalyWaitsForPlayerAndShowsRealOpening` | An enemy-first anomaly resolves its real opening before player guidance resumes. |
| `TC_TUT_006_DefeatDoesNotCompleteGuidedTutorial` | Defeat closes the current guidance without recording completion, so a fresh run can offer it again. |

The full `BattleSnapshotTests` Play Mode suite ran once before implementation and three times consecutively after implementation on isolated project/profile copies. Baseline: 152/152 passed, 0 failed/skipped, 136.471 s. Post-implementation: 158/158 passed, 0 failed/skipped in each of three consecutive runs (135.039 s, 131.485 s, 139.052 s). The baseline and all three post-implementation XML/log pairs are archived in `Docs/TestResults/2026-11-14-GuidedTutorial/`; see its `REPORT.md` for environment and exact artifact names.

These checks verify runtime flow and regression behavior in Play Mode. They do not replace a human visual/readability review at multiple window sizes or measure whether a new player learns the game more effectively; those remain follow-up evaluations.
