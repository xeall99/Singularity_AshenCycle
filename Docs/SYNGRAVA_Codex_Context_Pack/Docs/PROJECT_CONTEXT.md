# SYNGRAVA Project Context

Version 0.4
Updated 19 September 2026
Owner Akmal Maulana Ghani

## Purpose

This file is the machine-readable project context for Codex and other coding assistants working inside the local Unity repository. It summarizes approved design decisions, the current prototype, known differences between the prototype and the target design, and the rules for making safe changes.

This file provides persistent instructions and project memory. It does not train model weights. A future chat should read `AGENTS.md`, this file, the newest relevant source files, and the GDD before implementing a feature.

## Product Summary

SYNGRAVA is a single-player, turn-based dark-fantasy roguelite RPG for Windows. The player controls one active Warden, travels through a node-based run, gains temporary upgrades, and creates Convergence effects by arranging three combat actions in the Singularity Orbit.

The project is an SMK RPL UKK project built by one developer. Completion, stable database behavior, demonstrable OOP, readable UI, testing evidence, and a working Windows build have higher priority than optional online or content-heavy systems.

## Product Identity

| Field | Approved Value |
| --- | --- |
| Active title | SYNGRAVA |
| Previous working title | Singularity Ashen Cycle |
| Genre | Turn-based dark-fantasy roguelite RPG |
| Player format | One active Warden |
| Engine | Unity 6.6 `6000.6.0f1` |
| Render direction | Universal 2D first, staged 2.5D presentation later |
| Target platform | Windows PC 64-bit |
| Database | SQLite offline |
| Visual palette | Black, purple, and white |
| Core mechanic | Three-slot Singularity Orbit and Convergence |
| Mandatory mode | Standard Run |

## Scope Order

### Must Complete

- Main Menu, profile, Standard Run, node map, battle, reward, boss, and Run Summary.
- Locked Warden Base Stats and resettable Temporary Run Stats.
- Attack, Skill, Guard, mana validation, and enemy AI.
- Hidden enemy plan and Analyze.
- Singularity Orbit with at least two Named Patterns and Unstable Pulse fallback.
- SQLite content data, save data, at least one complete CRUD demonstration, and a Windows build.

### Complete After the Standard Run Is Stable

- Additional Named Patterns, Anomalies, relics, elite nodes, events, tooltips, tutorial, and accessibility polish.
- Infinite Run Casual, checkpoint resume, deterministic scoring, and a local top-ten leaderboard.

### Optional or Post UKK

- Infinite Run Ranked, authoritative global leaderboard, official seeded sessions, daily or weekly seeds, Memory Echo, and advanced Orbit manipulation.

### Excluded from the Current Scope

- Multiplayer, gacha, open world, large party combat, many Acts, full voice acting, live-service seasons, and a mandatory custom online backend.

## Locked Stats and Run Progression

Warden Base Stats belong to a ruleset and are not increased by character levels or permanent attribute points. Permanent progression unlocks skills, relics, Archive entries, presets, and other choices. It must not create a permanent raw-stat advantage at the start of a run.

Temporary Run Stats are modifier records owned by the active run. A modifier records its source, effect type, flat value, percent value, stack count, and scope. Effective values must be calculated from Base Stats plus active modifiers by one shared calculator.

Baseline formula:

`Effective Stat = (Base Stat + total flat bonuses) * (1 + total percentage bonuses)`

Round only at the final step defined by the stat calculator. Battle execution, UI preview, save restoration, Run Summary, and score validation must use the same calculation rules.

### Standard Run Stacking

- A Standard or Act run contains approximately 8 to 10 nodes.
- Eligible rewards can stack during that active run.
- The stack exists only until the run is Cleared, Defeated, or Abandoned.
- Starting a new run creates an empty run modifier collection.
- A reward must never overwrite the locked Base Stats record.

### Infinite Run Stacking

- Temporary upgrades may continue stacking without a fixed numerical stack cap during the active Infinite Run.
- Unlimited duration does not mean unlimited reward quality. Rarity remains controlled by depth eligibility and weighted chance.
- A high tier becoming eligible does not guarantee that it appears.
- When the Warden loses or abandons the Infinite Run, every Temporary Run Stat resets to the locked baseline.
- Only the final score, depth, build summary, history, and valid horizontal unlocks may persist.

## Battle Baseline

The current prototype uses these known values. They are implementation evidence, not final balance promises.

| Item | Current Prototype Value |
| --- | --- |
| Player Max HP | 100 |
| Player Max Mana | 10 |
| Starting Mana | 5 |
| Basic Attack damage | 10 in the current `BattleManager` prototype |
| Shadow Strike | 2 Mana and 20 Power from SQLite data |
| Guard | Reduces the next incoming damage by 50 percent and restores 2 Mana |
| Turn Mana recovery | 1 Mana after the enemy sequence in the current prototype |
| Current encounter count | Three sequential single-enemy battles |

The GDD ruleset table may contain later target values such as Attack, Defense, Speed, and Critical Chance. Do not silently mix those future domain-model values into the current prototype. Introduce a migration only when the battle model is ready.

## Singularity Orbit Contract

Orbit stores a maximum of three successful player action categories:

- `Attack`
- `Skill`
- `Guard`

Analyze, Item, failed input, cancelled target selection, and a skill rejected for insufficient Mana do not enter Orbit.

After the third successful action:

1. Resolve the full three-action sequence exactly once.
2. Apply the normal effect of the third action.
3. Apply the Convergence result exactly once.
4. Show the result through battle text and visual feedback.
5. Clear the three gameplay slots.
6. Continue the turn sequence only if battle has not ended.

Baseline implemented patterns:

| Sequence | Result | Prototype Effect |
| --- | --- | --- |
| Attack Attack Skill | Event Horizon | Third Skill gains 30 percent bonus damage |
| Guard Attack Guard | Reversal Orbit | Prepares a counter worth 50 percent of Basic Attack |
| Other sequence | Unstable Pulse | Restores 1 Mana, limited by Max Mana |

Arcane Collapse and Stable Core remain later content unless their code and tests already exist.

## Hover Only Convergence Preview

The preview must not display three permanent lines in the middle of the battle screen.

Required behavior:

1. Keep the preview hidden when fewer than two Orbit slots are occupied.
2. When two slots are occupied, hover or keyboard focus on Attack, Skill, or Guard requests a preview for that candidate third action.
3. Show one compact tooltip directly above the focused button.
4. Display the resulting Convergence name and a short numerical effect description.
5. Use `OrbitResolver.Preview` or the same pure resolver used by execution.
6. Hide the tooltip on pointer exit, focus loss, button click, enemy turn, modal opening, victory, defeat, or disabled input.
7. Analyze has no Convergence preview because it does not enter Orbit.
8. If Skill is disabled because Mana is insufficient, its hover feedback may explain the missing Mana but must not imply that the action can be executed.

## Hidden Enemy Plan and Analyze

Enemy intent is hidden during normal play. The game may prepare an action internally, but its type, damage, target, and effect are not shown before the player spends Analyze.

### Approved Analyze Option A

- Cost: `ceil(0.75 * EffectiveMaxMana)`.
- Example: with 10 Max Mana, Analyze costs 8 Mana.
- Limit: once per node.
- Turn use: Analyze does not consume the player's turn.
- Orbit: Analyze never adds, removes, or replaces an Orbit action.
- Result: after Analyze succeeds, the player may still choose Attack, Skill, or Guard.
- Reset: the once-per-node flag resets only when a new node begins, not every player turn.

### Analyze Validation Order

1. Confirm the battle is active and player input is permitted.
2. Confirm the current node has not already used Analyze.
3. Calculate the cost from Effective Max Mana and round upward.
4. Confirm current Mana is at least the calculated cost.
5. Deduct the Mana exactly once.
6. Mark Analyze as used for the node.
7. Reveal the already-prepared enemy action.
8. Keep the state in the player's action phase.

If any validation fails, Mana, turn, Orbit, and enemy plan remain unchanged.

### Stable Enemy Plan

The enemy action must be prepared once before the player chooses a combat action. Analyze reads the same immutable plan that the enemy later executes. The AI must not roll a new action after Analyze, because that would make the revealed information false.

Recommended data object:

`EnemyActionPlan { actionType, sourceEnemyId, targetId, baseDamage, hitCount, statusEffect, isLocked, planToken }`

For the current single-enemy prototype, Analyze can display that enemy's next action and estimated damage. For a future group encounter, it should display the total estimated incoming damage and a compact list of important non-damage effects.

### Baseline Enemy Actions

| Action | Meaning | Reveal Content |
| --- | --- | --- |
| Normal Attack | Standard damage using the enemy baseline | Name, target, hit count, and estimated damage |
| Heavy Attack | Higher damage with a clear internal multiplier | Name, target, hit count, and estimated damage |
| Charge | Prepares a stronger later action instead of normal immediate damage | Charge state and the action it prepares when known |

Exact Heavy Attack and Charge multipliers remain configurable balance values. Do not hard-code undocumented final numbers into several scripts.

### UI Migration Rule

The scene currently has an assigned `EnemyIntentText`. During the incremental migration, keep its serialized reference so the Inspector does not lose data. Clear or hide it by default, then reuse it as the temporary Analyze result display if practical. Rename or replace it only with an explicit scene migration plan, using `FormerlySerializedAs` when needed.

## Automatic Reward Selection

After a non-final victory, the reward flow starts automatically. The separate Choose Reward click is removed.

### Transition Sequence

1. Lock Attack, Skill, Guard, and Analyze input as soon as victory is resolved.
2. Show a brief victory state for approximately 0.8 seconds.
3. Hide or visually suppress battle commands and Orbit information that would overlap the reward screen.
4. Activate a full-screen black dim layer with approximately 70 percent opacity.
5. Generate three eligible, non-duplicate reward choices.
6. Position the three portrait cards below the visible screen.
7. Animate them upward into the center with a smooth eased motion and a short stagger.
8. Enable selection only after each card has reached a safe interactive state.
9. Apply one selected reward exactly once.
10. Save the completed reward transaction, close the modal, then continue to the next node or battle.

Use unscaled time for the modal animation if gameplay time is paused.

### Card Layout

Each reward card is a tall portrait panel rather than a wide rectangular button. It contains:

- illustration area or clear placeholder image;
- tier or rarity label;
- reward name;
- short mechanical description;
- exact numerical value;
- current stack count when the reward can stack;
- visible hover and keyboard focus state.

The visual direction uses original black, purple, and white assets with dark high-fantasy ornament and orbit motifs. Other games are references for hierarchy and usability only. Do not copy their artwork, typography, card frames, or layout pixel for pixel.

### Card Interaction

- Hover lifts and slightly scales the card.
- The border animates with a controlled purple orbit or traveling highlight.
- Pointer exit returns the card smoothly to its original position.
- One click locks all three cards before applying the reward.
- Repeated clicks cannot grant duplicate rewards or skip more than one node.
- Keyboard or controller focus must trigger the same information and border state.

## Reward Tier and Depth Rules

The reward system is seeded and weighted. Depth unlocks eligibility; a weighted roll determines the actual tier. The following names and thresholds are provisional tuning data and should live in configurable data rather than repeated conditionals.

| Tier | First Eligible Depth | Design Meaning |
| --- | ---: | --- |
| Tier 1 | 1 | Basic build foundation |
| Tier 2 | 5 | Early specialization |
| Tier 3 | 15 | Stronger focused modifier |
| Tier 4 | 30 | Build-defining combination support |
| Tier 5 | 60 | Mid-depth rare modifier |
| Tier 6 | 120 | Advanced Infinite modifier |
| Tier 7 | 250 | Deep-run modifier |
| Tier 8 | 500 | Very rare deep-run modifier |
| Tier 9 | 750 | Extreme-depth modifier |
| Tier 10 | 1001 | Highest tier becomes eligible, never guaranteed |

Standard Run normally reaches only early tiers because it ends after approximately ten nodes. Infinite Run can reach later eligibility thresholds while retaining low-tier results in the weighted pool. This prevents every late reward from becoming automatically top tier.

Each rarity should eventually have visually distinct original cards. The ten-tier content set is a long-term target. The UKK build may use fewer fully implemented tiers as long as the data structure already supports expansion and the scope is documented honestly.

## Save and Reset Rules

| Run State | Persistent Result | Temporary Build |
| --- | --- | --- |
| Active | Checkpoint, node, seed, current resources, modifiers | Restored exactly on Continue |
| Cleared | History, score, valid unlocks, summary | Deleted after the completion transaction |
| Defeated | History, depth, score, summary | Deleted before starting another run |
| Abandoned | Abandon record when required | Deleted and cannot leak into a new run |
| Corrupt | Recover last complete checkpoint or start clean | Unvalidated modifiers are never applied |

Checkpoint after a reward is selected and fully applied. Do not save a half-open reward selection as if the transaction were complete.

## Infinite Run and Leaderboards

Infinite Casual reuses the Standard Run combat, reward, node, save, and content systems. A cycle contains approximately ten nodes and ends with a boss. Defeating the boss starts the next cycle. Difficulty and eligible rewards increase from data-driven rules.

Infinite mode begins only after:

1. three consecutive Standard Runs complete without blocker errors;
2. Temporary Run Stats reset correctly after victory, defeat, and abandon;
3. Active Run save and resume restores the same state;
4. the score calculator is deterministic.

The local leaderboard stores at least the best ten results by mode, including score, depth, cycle, boss count, seed, duration, ruleset version, and date.

Global Ranked is optional or post UKK. A trustworthy global leaderboard requires authentication, official session or seed data, ruleset version validation, plausible-value checks, and server-side acceptance. A client must not be trusted merely because it uploads an integer score.

## Current Scene and Inspector Context

The observed Battle scene contains this structure:

```text
Battle
  Main Camera
  BattleManager
  Canvas
    Background
    PlayerBox
    EnemyBox
    PlayerHPText
    PlayerManaText
    EnemyHPText
    BattleLogText
    EnemyIntentText
    CommandPanel
      BasicAttackButton
      SkillButton
      GuardButton
      AnalyzeButton
    OrbitPanel
      OrbitRings
      OrbitSlot1Text
      OrbitSlot2Text
      OrbitSlot3Text
      ConvergencePreviewText
    ResultPanel
      ResultTitleText
      ResultDescriptionText
      RetryButton
      NextBattleButton
    RewardPanel
      RewardTitleText
      RewardButton1
      RewardButton2
      RewardButton3
  GameDatabase
  EventSystem
```

Names are evidence from the prototype scene. Inspect the actual Hierarchy before relying on them because the user may have made newer local changes.

## Known Runtime APIs

Preserve these current relationships unless the change updates every caller safely:

- `GameDatabase.Instance.IsReady`
- `GameDatabase.Instance.GetSkillByCode("SKL_SHADOW_STRIKE")`
- `GameDatabase.Instance.GetEnemyByBattleIndex(battleNumber)`
- `SkillData.Name`, `SkillData.ManaCost`, `SkillData.Power`
- `EnemyData.Name`, `EnemyData.MaxHP`, `EnemyData.AttackDamage`, `EnemyData.RewardEmbers`, `EnemyData.IsBoss`
- `OrbitState.AddAction`, `OrbitState.IsFull`, `OrbitState.Actions`, `OrbitState.Clear`, `OrbitState.Count`, `OrbitState.TryGetAction`
- `OrbitResolver.Resolve` and `OrbitResolver.Preview`
- `BattleVisuals.PlayPlayerAttack`, `PlayEnemyAttack`, `PlayEnemyHit`, `PlayPlayerHit`, and `PlayGuard`

Do not redefine existing enums or data classes in `BattleManager.cs`. Search the repository before adding a type.

## Current Implementation Status

### Working Prototype Evidence

- SQLite skill and enemy data can be read.
- Basic Attack, Shadow Strike, Guard, enemy turn, victory, defeat, and three sequential battles have worked in Play Mode.
- Orbit stores action categories and resolves Event Horizon, Reversal Orbit, and Unstable Pulse.
- Orbit text slots, procedural rings, and basic placeholder animation have appeared in Play Mode.
- Three temporary-stat reward choices have appeared and can affect the current run prototype.
- A Git repository and initial commit exist locally.
- `AnalyzeButton` exists in the Battle scene Hierarchy.
- EnemyActionPlan, hidden intent, and Analyze are implemented and verified by 23 automated Unity Play Mode cases on 12 September 2026. See the verification record below; this does not mark all Battle UX v0.4 features complete.
- Hover/focus Convergence preview, automatic post-stage rewards, and portrait reward cards are implemented and verified together with Analyze by 29 automated Unity Play Mode cases on 12 September 2026. See the current Battle UX verification record below.
- Seeded weighted reward tiers, no-duplicate reward generation, and explicit stack presentation are implemented and verified together with the earlier Battle UX behavior by 31 automated Unity Play Mode cases on 12 September 2026.
- Resource carryover between nodes is implemented and verified with all earlier Battle UX behavior by 40 automated Unity Play Mode cases on 12 September 2026. Current HP/Mana survive reward selection; only a new run or Restart Run initializes resources.
- The approved reward/Orbit follow-up and `RunModifierCollection` are implemented and verified with all earlier behavior by 42 automated Unity Play Mode cases on 13 September 2026.

### Implemented in Battle UX Version 0.4

- Attack, Skill, and Guard show one hover/focus-only Convergence tooltip above the relevant command when Orbit contains exactly two actions.
- A non-final victory opens the reward selection automatically after a short unscaled delay; no Choose Reward input is exposed.
- The three existing reward buttons are presented as portrait cards with a black translucent dim layer, staggered entrance, exact reward/stack labels, and pointer/focus border feedback.
- `RewardManager` creates one three-choice snapshot per cleared node from distinct `RewardDefinition` entries. Tier eligibility follows the GDD depth thresholds, and the same snapshot drives card text, glyphs, exact values, and effect application.

### Planned Next

- Authored portrait art to replace the current procedural Singularity card illustrations.
- `RunStatController`, followed by seeded `RunManager` state and the Standard Run node map.

### Known Technical Debt and Design Differences

- `BattleManager` currently owns too many responsibilities. `RunModifierCollection` now owns active modifier records, while the existing private aggregate fields remain synchronized for compatibility. Continue extraction incrementally.
- The reward pool currently contains the three verified stat effects. Generation and tier selection are data-driven and seeded, but relics, Anomalies, and a larger database-backed reward pool remain planned.
- Enemy intent is now hidden by default and Analyze reveals an immutable Normal Attack snapshot. Heavy Attack, Charge, and multi-enemy execution remain planned.
- Convergence preview is hover/focus-only and uses `OrbitResolver.Preview`; the former permanent multi-line text in the Orbit panel is no longer displayed.
- `OrbitUIAnimator` still detects ordinary slot text changes, while completed Convergence now sends an explicit pulse before Orbit clears.
- The procedural Orbit ring graphic is a functional placeholder and still needs visual polish.
- Reward portraits use procedural Singularity rings and letter glyphs because no authored reward portrait assets exist yet.
- The GDD defines tier thresholds but does not define numeric weights or value multipliers. `RewardManager.CreateDefaultTierConfigurations` contains provisional centralized balancing values; they must move to content data when the production reward repository is introduced.
- `battleNumber` is the current depth fallback and the run seed exists only for the active runtime session. `RunManager`, checkpoint persistence, and same-seed resume are not implemented yet.
- The current battle prototype supports one enemy at a time. Group Analyze output is a future-compatible requirement, not proof of current group combat.
- `StartBattle` carries current HP/Mana between nodes and clamps them to their current maximum. Following the user's 13 September decision, Vital Core now adds its exact flat value to both Effective Max HP and current HP. This state still belongs to the live Battle scene; cross-scene persistence, Rest rules, and checkpoint restore await the run systems.
- Reward card labels are resolved separately from portrait glyph text and include inactive children safely.
- Reward persistence/save-after-selection is not implemented because save/checkpoint work remains outside this Battle UX slice.
- Full Infinite generation, resume, local leaderboard, Ranked validation, and global leaderboard are designs, not completed features.

## File Ownership Direction

| Responsibility | Preferred Owner |
| --- | --- |
| Battle phase and results | `BattleManager` initially, later a focused battle state controller |
| Action animation timing | `BattleVisuals` and later `ActionSequencer` |
| Three action memory | `OrbitState` |
| Convergence result | `OrbitResolver` |
| Orbit presentation | `OrbitUIAnimator` and `SingularityOrbitRings` |
| Prepared enemy action | `EnemyActionPlan` and `EnemyAI` |
| Analyze validation | Focused Analyze controller or BattleManager integration during prototype |
| Reward generation | `RewardManager` |
| Card transition and hover | `RewardSelectionUI` and `RewardCardView` |
| Run modifiers | `RunStatController` or `RunModifierCollection` |
| Run lifecycle | `RunManager` |
| Save and recovery | `SaveService` and repositories |
| Score | Pure `ScoreCalculator` |

Avoid a large rewrite solely to match these target names. Introduce classes when their responsibility is actually being extracted and tested.

## Acceptance Tests for Battle UX Version 0.4

### Analyze

- With Max Mana 10 and Mana 8, Analyze spends 8 Mana, reveals the prepared action, and leaves player action buttons available.
- With Mana 7, Analyze is rejected without changing Mana, turn, Orbit, or the prepared enemy plan.
- A second Analyze in the same node is rejected without cost.
- Analyze does not fill any Orbit slot.
- A new node resets the Analyze limit.
- The enemy executes the same plan that Analyze revealed.

### Hover Preview

- At Orbit count 0 or 1, no Convergence tooltip appears.
- At Orbit count 2, each valid action shows only its own result while hovered or focused.
- The tooltip is positioned above the correct button and remains within the Canvas.
- Pointer exit and focus loss hide it.
- The actual third-action result matches the preview.

### Reward Modal

- A non-final victory opens three rewards automatically after the configured delay.
- No Choose Reward input is required.
- The dim background is black and remains partially transparent.
- Three different cards enter from below with smooth staggered motion.
- Hover and focus animate the relevant card border.
- Battle input is blocked while the modal is active.
- One selection applies exactly once, saves after completion, and advances exactly one node.
- Final boss victory opens Run Summary instead of an ordinary reward.
- A reward screen contains three different codes, remains stable when presentation refreshes, and regenerates deterministically from the same seed and depth.
- Tier 10 is ineligible at depth 1000, becomes eligible at depth 1001, and is never guaranteed by eligibility alone.

### Regression

- Attack, Skill, Guard, Mana, enemy turn, victory, defeat, Retry, Restart Run, Orbit reset, and all Inspector references still work.
- Unity Console contains no red error on the tested flow.
- The same flow works in a Windows development build before the milestone is marked complete.

## Codex Working Procedure

Before edits, Codex must inspect the repository and state the smallest plan. It must preserve unrelated changes, serialized fields, `.meta` files, scene references, and database data. It must not claim Unity compilation or Play Mode success unless Unity actually performed that check.

After edits, Codex must report exact files changed, Inspector assignments, automated checks, remaining Unity tests, known limitations, and a suggested small commit message. Accepted decisions and implementation status should then be recorded here and in the GDD change log.

## Enemy Action Snapshot Verification 12 September 2026

### Implemented Behavior

- `EnemyActionPlan` is a plain immutable C# class. It copies enemy ID and AttackDamage into a Normal Attack plan with target Warden, one hit, multiplier 1, and no additional effect.
- `StartBattle` clears the previous plan and Analyze result, resets the node-use flag, then prepares one plan before player input. The current deterministic Normal Attack repeats that same object throughout the node. Analyze, Attack, Skill, Guard, and EnemyTurn do not prepare or reroll it.
- `enemyIntentText` remains assigned. It is cleared and hidden in Awake/start, then displays action, target, hits, estimated base damage, and effect after Analyze. Its existing text rectangle grows vertically at runtime to fit the result. Enemy execution, battle end, and errors hide the result again.
- Analyze deducts `ceil(0.75 * EffectiveMaxMana)` once, keeps the player's turn, and never changes Orbit. Until the Effective Stats system exists, `playerMaxMana` is the documented fallback. Integer calculation `M - M / 4` is exact for non-negative integer M. Max Mana 10 costs 8: Mana 8 becomes 0, Mana 7 remains 7 on rejection, and Mana 10 becomes 2.
- EnemyTurn reads `TotalEstimatedDamage` from the saved snapshot. Guard modifies received damage only; Reversal counter behavior is unchanged. The estimate is base incoming damage before Guard.
- Missing/invalid plans stop battle with an explicit BATTLE ERROR and Restart Run action. There is no silent replacement plan. Missing-plan Analyze does not charge Mana.
- `EndBattle` ignores repeated completion calls so Embers/results are not applied twice. Existing reward selection and resource-reset behavior are retained.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **23 passed, 0 failed, 0 skipped**. Unity exited with code 0. Gameplay and test source files in the tested copy matched the repository byte-for-byte at verification time.

The suite loads the actual Battle scene in an isolated copy of the project, uses the already-resolved package sources, and gives the copy a separate company/product identity to protect the user's runtime database. Tests invoke real button listeners or command methods, inspect runtime state, and let enemy coroutines execute. Time scale is accelerated for automation; mouse navigation and visual polish were not manually tested.

| Test | Result | Evidence checked |
| --- | --- | --- |
| TC-ANL-001 | PASS | Mana 8 becomes 0; prepared plan visible; Analyze disabled; turn/Orbit unchanged |
| TC-ANL-002 | PASS | Mana 7 rejected with required message; state/plan unchanged; retry succeeds after Mana reaches 8 |
| TC-ANL-003 | PASS | Repeated use cannot charge again or replace the snapshot, even after Mana is replenished |
| TC-ANL-004 | PASS | Changing EnemyData.AttackDamage after reveal does not change actual base damage; subsequent turn retains the same snapshot |
| TC-ANL-005 | PASS | Victory/reward advances one node, creates a new plan, resets Analyze, and clears the previous reveal |
| TC-ANL-006 | PASS | Two existing Attack entries remain identical; no third Orbit entry or Convergence |
| TC-ANL-007 | PASS | Waiting after Analyze does not start enemy action; valid Attack/Skill/Guard remain available |
| Eight cost cases | PASS | Upward rounding including Max Mana 10, 11, 12 and integer maximum |
| Battle/Orbit regression | PASS | Attack, Skill cost/rejection, input lock, Guard, enemy damage, Reversal, Event Horizon/preview, Unstable Pulse, victory, defeat, Restart Run, next battle, and final Run Complete |
| Missing-plan cases | PASS | Two deliberately injected errors are explicitly handled without reroll; expected error logs are asserted |
| Reference/immutability checks | PASS | Snapshot properties have no setters; all serialized Unity object references remain assigned; Basic Attack stays 10 |
| Manual visual/input review | NOT RUN | Automated tests verify text/state and listeners, not mouse hit areas or visual composition |
| Windows standalone development build | NOT RUN | This task verified Editor compilation and Play Mode; standalone remains a release gate |

No unexpected Error/Exception logs were accepted by the Play Mode suite. Existing package warnings occurred during the initial import. The test API deprecation warning was corrected before the final passing run.

Reproduce from the repository root with `./Tools/Run-BattleSnapshotTests.ps1`. It creates a temporary project and prints the Unity log and XML paths. The suite intentionally skips execution under the production product name because GameDatabase currently overwrites its runtime database from the seed in Editor.

The retained final report is `Docs/TestResults/EnemyActionPlan-PlayMode.xml` (repository-root-relative). Its timestamps are UTC and individual test records contain runtime evidence.

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs` and this context file at `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md`.
- Added: `Assets/Project/Scripts/Battle/EnemyActionPlan.cs` and its `.meta`.
- Added: `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, `Syngrava.Battle.PlayModeTests.asmdef`, and the corresponding asset/folder metadata.
- Added: `Tools/Run-BattleSnapshotTests.ps1` and `Docs/TestResults/EnemyActionPlan-PlayMode.xml`.
- The Battle scene was not edited by this milestone. All 33 existing serialized fields and their names were preserved. `analyzeButton` and `enemyIntentText` already reference the existing objects; no Inspector assignment or model component attachment is required.
- Existing local Battle scene/Orbit changes were preserved. Additional TMP fallback-font and solution-file changes observed during the session were left intact, not reverted. No package, SQLite schema, or GDD design change was made.

### Remaining Scope

This completes the EnemyActionPlan/Hidden Intent/Analyze implementation and automated verification slice. It does not complete all Battle UX v0.4 or replace the Windows-build/manual-review gate.

Normal Attack is the only executable plan type; no multi-hit, Heavy Attack, Charge, status system, or multi-enemy behavior is claimed. Node identity is still the prototype battle index. At this verification point StartBattle reset resources; the Resource Carryover verification below supersedes that limitation. Analyze becomes unused on a new node but remains disabled until Mana is sufficient.

The hover/reward/card follow-up named here was completed later on the same date; the current record follows below. Save, node map, Infinite, and leaderboards remain planned.

## Battle UX Version 0.4 Verification 12 September 2026

### Implemented Behavior

- `BattleCommandMenu` keeps its existing Pointer Enter/Exit, Select/Deselect, and press trigger path. It publishes one preview request or hide signal; `BattleManager` subscribes once and remains the only owner of `OrbitResolver.Preview`. Pointer and keyboard focus are tracked separately so leaving a hovered button does not discard a still-focused command.
- The existing serialized `convergencePreviewText` reference is retained and reparented only at runtime into `ConvergenceHoverTooltip`. The tooltip is hidden at Orbit counts 0 and 1. At count 2 it shows only the hovered/focused Attack, Skill, or Guard result, follows that button, clamps to the Canvas, ignores raycasts, and clears on exit, focus loss, action execution, turn change, battle result, or reward modal.
- `UpdateOrbitUI` no longer writes permanent `ORBIT n/3` or all-three preview lines into the center of the Orbit panel. The three slot references remain unchanged. `OrbitUIAnimator` now recognizes the em dash empty marker and receives an explicit Convergence pulse before the Orbit state clears.
- A non-final victory keeps the Victory result visible for 0.8 real-time seconds, then opens rewards automatically. `nextBattleButton` and its listener remain assigned for compatibility, but the button is hidden and no Choose Reward click is required. Final victory continues directly to Run Complete.
- Reward selection uses the existing panel, title, and three button references. The modal background is black at 72% alpha. The three buttons become 280x410 portrait cards and enter from below with staggered unscaled animation. All reward input remains locked until the entrance completes; keyboard/gamepad focus then starts on the first card.
- Each card has a procedural Singularity illustration, a stable A/V/G glyph, Tier I name, exact effect value, and current-to-next stack count. Pointer hover and UI focus lift/scale the card and brighten its border. Authored portrait sprites are still a future art pass.
- `rewardSelectionCommitted` and the existing end-battle guard ensure multiple callbacks cannot apply more than one reward or advance more than one node. Closing the modal clears its UI focus, restores command/Orbit presentation for the new node, and leaves Analyze/node snapshot reset behavior intact.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **29 passed, 0 failed, 0 skipped** in 14.63 seconds. Unity exited with code 0. The Battle scene and all production/test source files listed below matched the repository byte-for-byte in the isolated test project.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, insufficient Mana, one-use limit, deterministic execution, node reset, unchanged Orbit, and unchanged turn |
| Analyze rounding matrix | PASS | Eight integer cases, including Max Mana 10 -> cost 8 and `int.MaxValue` without overflow |
| TC-HVR-001 | PASS | Orbit 0/1 never reveals Convergence preview |
| TC-HVR-002 | PASS | Attack/Skill/Guard each show only their own resolver result; tooltip stays above its button and inside Canvas; exit hides it |
| TC-HVR-003 | PASS | Pointer/focus overlap restores the focused preview; focus loss hides it; executed Event Horizon matches the preview |
| TC-RWD-001 | PASS | Non-final Victory displays first, reward opens automatically, Choose Reward stays hidden, battle UI is blocked/hidden, dim layer is black 72%, and cards start below Canvas while locked |
| TC-RWD-002 | PASS | Three distinct portrait cards, stable glyph/label separation, Tier/value/stack text, completed entrance, first-card controller focus, pointer hover, focus lift, border feedback, and reset |
| TC-RWD-003 | PASS | Two rapid callbacks apply Attack reward once and advance exactly one node |
| Battle/Orbit regression | PASS | Attack, Skill, Guard, Mana, enemy plan/damage, Reversal, Event Horizon, Unstable Pulse, victory, defeat, Restart Run, automatic node advance, and final Run Complete |
| Missing-plan and reference checks | PASS | Expected snapshot errors remain explicit; no silent reroll; all serialized Unity object references remain assigned; snapshot remains immutable; Basic Attack remains 10 |
| Unexpected Console errors/exceptions | PASS | No unexpected matching error, exception, or compiler-error entries; the two deliberately injected missing-plan errors were expected and asserted |
| Manual visual/mouse review in an interactive Editor | NOT RUN | Position, state, pointer/focus dispatch, and animation end states were automated; final art composition still needs human visual review |
| Windows standalone development build | NOT RUN | Editor compile and Play Mode passed; a standalone development build remains a release gate |
| Save-after-reward persistence | NOT RUN | Save/checkpoint infrastructure is not part of this milestone and remains planned |

Reproduce from the repository root with `./Tools/Run-BattleSnapshotTests.ps1`. The retained current report is `Docs/TestResults/BattleUX-v0.4-PlayMode.xml`. The earlier 23-case snapshot report remains in place as historical evidence.

### Files and Inspector

- Modified for this slice: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Project/Scripts/UI/BattleCommand.cs`, `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added for this slice: `Assets/Project/Scripts/Battle/RewardSelectionUI.cs`, `Assets/Project/Scripts/Battle/RewardCardView.cs`, their `.meta` files, and `Docs/TestResults/BattleUX-v0.4-PlayMode.xml`.
- The Battle scene was not edited by this slice. Existing local scene changes were preserved and the current scene file was the exact one loaded by the passing suite.
- No Inspector assignment is required. `convergencePreviewText`, `nextBattleButton`, `rewardPanel`, `rewardTitleText`, and all three reward buttons keep their existing serialized references. Runtime presentation components are added idempotently to those existing objects.
- No package manifest, SQLite schema/database, base-stat value, GDD rule, or existing `.meta` file was changed.

### Remaining Scope

This completes the requested hover preview, automatic reward transition, and portrait-card presentation slice in Editor Play Mode. It does not claim authored reward art, weighted reward generation, persistence, Windows build validation, or completion of the wider run systems.

The weighted tier and resource carryover slices named above were completed later on the same date; see their verification records below. Heavy Attack, Charge, multi-enemy, node map, save/checkpoint, Infinite Run, and leaderboard remain planned.

## Weighted Reward Tier Verification 12 September 2026

### Implemented Behavior

- `RewardDefinition` is an immutable reward snapshot containing code, name, description, glyph, effect type, tier, eligibility depth, roll weight, exact flat/percent value, and presentation text. UI and effect application read the same object.
- `RewardManager` centralizes all ten GDD tier thresholds: nodes 1, 5, 15, 30, 60, 120, 250, 500, 750, and 1001. Eligibility expands the weighted pool and never guarantees the highest eligible tier.
- The GDD does not specify numeric tier weights or value multipliers. The manager therefore owns one provisional configuration table instead of scattering balancing literals through battle/UI code. Tier I retains the existing Inspector baselines: Attack +10%, Max HP +15, and Guard Strength +20%.
- A run seed and battle depth deterministically generate one three-choice snapshot. The three reward codes are unique, their order is seeded, and repeated UI configuration does not generate new choices.
- The existing three button listeners remain registered exactly once. Their legacy private callback names now select snapshot indices, so randomized card order still applies the displayed effect. A stale rapid click returns before inspecting a cleared snapshot.
- Stack counters are stored separately per reward effect. Card labels show tier, name, exact numeric effect, description, and current-to-next stack count. Restart Run clears all reward modifiers and counters.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **31 passed, 0 failed, 0 skipped** in 16.76 seconds. Unity exited with code 0. All six production/test source files involved in this slice matched the isolated test project byte-for-byte.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, snapshot execution, node reset, unchanged Orbit, and unchanged turn |
| TC-HVR-001 through TC-HVR-003 | PASS | Hidden preview at Orbit 0/1, per-command preview at Orbit 2, pointer/focus lifecycle, and resolver parity |
| TC-RWD-001 through TC-RWD-003 | PASS | Automatic modal, input lock, portrait/card animation, dynamic reward data, rapid-click guard, one application, one node advance, and next-screen stack presentation |
| TC-RWD-005 | PASS | Tier 10 absent from eligibility at depth 1000, eligible at 1001, possible in seeded rolls, and not guaranteed |
| Seed/no-duplicate snapshot | PASS | Same seed/depth produces the same ordered codes and tiers; all three codes differ; presentation refresh keeps the same snapshot object |
| Battle/Orbit regression | PASS | Attack, Skill, Guard, Mana, enemy snapshot/damage, Reversal, Event Horizon, Unstable Pulse, victory, defeat, Restart Run, reward advance, and Run Complete |
| Serialized references and duplicate types | PASS | Existing Inspector references remain assigned; no duplicate reward/enemy/orbit class or enum; Basic Attack remains 10 |
| Unexpected Console errors/exceptions | PASS | No unexpected errors or exceptions were accepted by the final Play Mode run |
| Windows Development Build | PASS | Unity built `MainMenu.unity` and `Battle.unity` for Windows x86_64 with Development enabled; BuildResult Succeeded, 0 errors, output size 214,971,951 bytes |
| Interactive visual review | NOT RUN | Automated tests cover layout state and input events; authored art and final human composition review remain pending |
| Save/checkpoint restore | NOT RUN | The current session seed, reward stacks, and modifier values are not persisted because `RunManager` and checkpoint repositories remain planned |

The final Play Mode report is retained at `Docs/TestResults/RewardTier-v0.4-PlayMode.xml`. The Windows build was produced only in the isolated temporary project so no build artifact or temporary Editor script was added to this repository.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RewardDefinition.cs`, `RewardManager.cs`, and their `.meta` files.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `RewardSelectionUI.cs`, `RewardCardView.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- The Battle scene and all existing serialized fields were preserved. No new scene component, Inspector assignment, package, database schema, or GDD rule change is required.
- Existing `attackRewardPercent`, `maxHPRewardAmount`, and `guardRewardPercent` fields remain the Tier I baseline. All runtime card objects and glyphs are still created idempotently.

### Remaining Scope and Next Milestone

The current reward pool has three stat effects and procedural portraits. Relics, benefit-and-consequence Anomalies, authored card art, checkpoint save, and database-backed reward content remain planned. The provisional weights and multipliers require balancing data before production content lock.

The resource carryover and `RunModifierCollection` follow-ups were completed later, as recorded below. The next smallest milestone is `RunStatController`, then seeded `RunManager` state, before node map and checkpoint integration.

## Resource Carryover Verification 12 September 2026

### Approved Rule and Implemented Behavior

- The early prototype baseline in the GDD starts a battle at 5/10 Mana, while the later run-state contract carries current resources between nodes. Following the context authority order, 5/10 starting Mana and full HP now apply only to a new run or Restart Run. No GDD design rule or balancing value was rewritten.
- `Start` enters through the existing `RestartRun` path after database validation. That path clears temporary rewards/stacks, initializes a fresh reward seed, then sets HP to current maximum and Mana to the configured starting value clamped within Max Mana.
- `StartBattle` preserves current HP/Mana and only clamps out-of-range values. Resource initialization no longer runs after reward selection. The existing commit guard still applies a reward once and advances exactly one node.
- At this verification point Vital Core increased Effective Max HP without adding current HP. The user's 13 September decision supersedes that behavior; see the current follow-up record below. Current Mana carryover remains unchanged.
- Every new node still clears Orbit, prepares a new enemy snapshot, hides intent, and resets Analyze usage. Mana 0 remains insufficient for Analyze until recovered through an existing valid resource action.
- Basic Attack remains 10. Max HP 100, Max Mana 10, starting Mana 5, Skill cost, Guard recovery, enemy-turn recovery, and Convergence effects retain their existing baselines. `playerMaxMana` remains the documented effective-maximum fallback until a run-stat controller exists.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **40 passed, 0 failed, 0 skipped** in 26.17 seconds; Unity exited with code 0. This includes nine new resource cases and all 31 prior battle/reward cases. The tests load the actual Battle scene in an isolated copy with a separate product/company identity to protect the user's runtime database. BattleManager, the test source, and Battle.unity matched the tested copy byte-for-byte.

| Test | Result | Evidence checked |
| --- | --- | --- |
| TC-RES-001 | PASS | Only a new run initializes full HP/starting Mana; node entry preserves 43 HP and 8 Mana with matching UI |
| TC-RES-002 | PASS | All three reward effects preserve spent HP/Mana; Mana 0/7/10, rapid selection, new plan, hidden intent, reset Orbit/Analyze, button availability, and no delayed refill |
| TC-RES-003 | PASS | Historical 12 September behavior increased capacity without healing; superseded by the 13 September Vital Core rule below |
| TC-RES-004 | PASS | Skill victory preserves its Mana cost and Unstable Pulse recovery into the next node; Guard and completed enemy-turn recovery still work |
| TC-RES-005, three cases | PASS | Out-of-range HP/Mana clamp to their bounds while valid low resources remain unchanged |
| TC-RES-006, two cases | PASS | Restart clamps configured starting Mana below zero or above Max Mana without changing the Max Mana stat |
| TC-ANL-001 | PASS | Mana 8 pays 8 and reveals the stored plan |
| TC-ANL-002 | PASS | Mana 7 rejects Analyze without state changes |
| TC-ANL-003 | PASS | Repeated Analyze never charges or replaces the snapshot |
| TC-ANL-004 | PASS | Enemy execution uses the revealed snapshot even when source enemy data changes |
| TC-ANL-005 | PASS | Reward advances once, resets Analyze, preserves spent Mana at 0, and requires sufficient Mana before Analyze is available again |
| TC-ANL-006 | PASS | Analyze leaves two Orbit actions unchanged |
| TC-ANL-007 | PASS | Analyze leaves the player's turn and valid commands available |
| Battle, Orbit, hover, and reward regression | PASS | Attack, Skill, Guard, recovery, three Convergences, victory, defeat, retry/restart, final victory, preview parity, card transition, and seeded reward selection |
| Inspector compatibility and unexpected Console errors | PASS | All 33 serialized fields retain their definitions; assigned references stay valid; no duplicate types/listeners introduced; no unexpected test errors or exceptions |
| Windows Development Build | PASS | Current source built MainMenu and Battle for Windows x86_64 with Development enabled; Succeeded, 0 errors, 510 warnings, 214,971,862 bytes; Unity exited with code 0 |
| Interactive Editor/standalone playthrough | NOT RUN | Automated Play Mode covers controlled node transitions; manual input, visual review, and an unassisted full run are not claimed |
| Save/reload and full Standard Run | NOT RUN | Persistent run state and the complete 8-10 node Standard Run are not implemented in this prototype |

Report: `Docs/TestResults/ResourceCarryover-v0.4-PlayMode.xml`.

The Windows build and its temporary Editor helper were produced only in the isolated test copy. The 510 build warnings remain recorded; this is not a warning-free build. No interactive standalone playthrough is claimed.

### Files and Inspector

- Modified in this slice: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added: `Docs/TestResults/ResourceCarryover-v0.4-PlayMode.xml`. No runtime class, field, enum, or listener was added.
- Battle.unity, its existing references, and the checked script/scene `.meta` files were preserved. Analyze is already connected; no Inspector or Hierarchy setup is required.
- Existing unrelated Git changes were preserved. No package, database, generated folder, or project setting was edited in the repository.

### Known Limitations and Next Milestone

Carryover is currently in-memory within the Battle scene. Scene reload still starts a new run; future RunManager/save integration must own continuity across scenes. There is no new Rest/healing reward, max-Mana modifier system, status-effect lifecycle, or balance adjustment. Automated tests use controlled HP/enemy HP to exercise transitions and do not certify whole-run difficulty.

The repository's default EditorBuildSettings still references the obsolete SampleScene path. Build verification must explicitly include MainMenu and Battle until a separate build-configuration task updates that list. Building a player does not prove a manual standalone playthrough.

`RunModifierCollection` was completed in the follow-up below. Continue with `RunStatController`, followed by seeded `RunManager` state. Preserve the current serialized fields and public API during each extraction. Node map, checkpoints, Infinite Run, leaderboard, authored portrait art, and broader content remain planned.

## Reward, Orbit, and Run Modifier Follow-up 13 September 2026

### Approved Behavior

- Vital Core applies one immutable reward snapshot value to two resources. `Max HP +N | HP +N` adds `N` to the active run's Effective Max HP and adds the same `N` to current HP, capped by the new maximum. Example: 64/100 with a +15 card becomes 79/115. The existing reward commit guard prevents rapid clicks from applying either increase twice.
- Reward cards have no automatic initial focus, so opening the modal does not glow a card before pointer or navigation focus reaches it. Pointer and focus requests are coordinated across all three cards; activating one card immediately clears the other two visual states.
- OrbitPanel remains the same scene object with the same three assigned TMP references. It is now anchored left of PlayerBox, with slot 1, 2, and 3 arranged vertically from top to bottom. The existing procedural rings are rotated into a vertical frame. Orbit recording and Convergence resolution are unchanged.
- `RunModifierCollection` owns immutable active-run modifier records by reward source. Each record stores source code, effect type, accumulated flat/percent value, stack count, and `ActiveRun` scope. Repeated rewards stack into the same source record. Effective values, card stack counts, and summary output read the collection; Restart Run clears it.
- The six existing private aggregate/stack fields remain present as synchronized compatibility projections. All 33 serialized fields, scene references, public APIs, and nine button listener registrations remain unchanged.

### Node Status

After a non-final victory, reward selection commits once, increments `battleNumber`, and `StartBattle` loads the next enemy by `BattleIndex`. The seed database currently supports the existing three sequential single-enemy battles, with the last battle acting as the prototype run boss/completion. A selectable node map and the complete Standard Run path of approximately 8-10 nodes have not been implemented. They remain dependent on `RunStatController` and seeded `RunManager` state.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **42 passed, 0 failed, 0 skipped** in 26.74 seconds; Unity exited with code 0. Windows x86_64 Development Build using MainMenu and Battle: **PASS**, 0 errors, 494 warnings, 214,976,367 bytes. The production scripts, scene, test source, and new `.meta` in the isolated run matched the repository byte-for-byte.

| Test | Result | Evidence checked |
| --- | --- | --- |
| TC-RWD-002 | PASS | No initial selected/glowing card; resting borders/scales; pointer and keyboard focus each highlight exactly one card; switching cards clears the previous state |
| TC-RES-002 | PASS | All rewards retain node carryover; Vital Core adds the same exact flat value to current and maximum HP; rapid selection applies it once |
| TC-RES-003 | PASS | Two Vital Core selections add current and maximum HP on both nodes, retain locked base Max HP, and reset all temporary values on Restart Run |
| TC-RUN-001 | PASS | Three reward effects create three active-run source records; totals and compatibility fields agree; repeated Vital Core stacks one record; reset empties the collection |
| TC-ORB-001 | PASS | Orbit panel is entirely left of PlayerBox and the three active slot references form a descending vertical column |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, immutable enemy snapshot, new-node reset, unchanged Orbit, and unchanged player turn |
| Full battle/UX regression | PASS | Attack, Skill, Guard, Mana, enemy execution, all Convergences, hover preview, automatic reward, victory, defeat, retry/restart, carryover, and three-battle completion |
| Serialized references, listeners, duplicate types, and unexpected test errors | PASS | 33 serialized fields and 9 add/remove listener pairs retained; no duplicate modifier/enemy/orbit class or enum; no unexpected test errors/exceptions |
| Windows Development Build | PASS | MainMenu and Battle built for Windows x86_64 with Development enabled; Unity reported Succeeded and 0 errors |
| Manual mouse/visual review in an interactive build | NOT RUN | Automated input events and numeric layout constraints passed; final human inspection of glow and composition is still recommended |
| Selectable node map and full 8-10 node Standard Run | NOT RUN | Only three sequential database battles currently exist |

Report: `Docs/TestResults/RewardOrbitFix-v0.4-PlayMode.xml`.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RunModifierCollection.cs`, its `.meta`, and `Docs/TestResults/RewardOrbitFix-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `RewardManager.cs`, `RewardSelectionUI.cs`, `RewardCardView.cs`, `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No Inspector assignment is required. OrbitPanel, OrbitSlot1Text, OrbitSlot2Text, OrbitSlot3Text, reward buttons, and every prior serialized reference keep their existing file IDs and connections.
- No package, SQLite schema/database, locked Base Stat, or existing `.meta` file was changed.

### Next Milestone

Implement `RunStatController` as the shared calculator over locked Base Stats and `RunModifierCollection`. After that, move run depth, seed, current HP/Mana, and modifier ownership into a seeded `RunManager`; then implement the complete Standard Run node map.

## Authored Orbit Frame and Action Icon Follow-up 13 September 2026

### Approved Behavior

- The vertical Orbit layout remains to the left of PlayerBox. Its procedural ring component and GameObject are preserved for compatibility, but the component is disabled so it cannot overlap the authored frame.
- `SyngravaOrbitFrame.png` supplies the three rough square slots and surrounding eye/shard ornament. The texture has real transparency, uses a black-purple-white palette, ignores raycasts, preserves aspect ratio, and renders behind the slot content.
- Orbit slots no longer display font glyphs. The three existing `orbitSlot1Text`, `orbitSlot2Text`, and `orbitSlot3Text` objects, serialized fields, and file IDs remain active and continue to store the compatibility labels `A`, `S`, `G`, or the empty marker. Their TMP render components are disabled at runtime.
- Three centered, non-raycast `Image` children render authored action symbols. Attack uses a radial singularity strike, Guard uses an eye-crested shield, and the current `SKL_SHADOW_STRIKE` skill uses its own crescent-blade symbol. An unmapped skill uses a neutral vortex fallback instead of a misleading hard-coded Shadow Strike image.
- `BattleManager.RecordOrbitAction` passes the recorded slot index and exact skill code to `OrbitUIAnimator`. The animator keeps skill identity per slot and resolves it through a serialized code-to-sprite catalog. Adding a later skill only requires its sprite and catalog entry; `OrbitState`, `PlayerActionType`, and `OrbitResolver` remain unchanged.
- Slot icon and legacy TMP state are synchronized by the presentation API itself. Clearing Orbit removes all three sprites and labels together. Analyze still does not enter Orbit, and Convergence still clears Orbit after the third valid Attack, Skill, or Guard.
- The rejected Rubik Dirt Orbit font assets and their obsolete test report were removed because this approved direction uses symbols.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Final automated Play Mode: **43 passed, 0 failed, 0 skipped** in 26.84 seconds. Windows x86_64 Development Build of MainMenu and Battle: **PASS**, Succeeded with 0 errors, 499 warnings, and output size 224,150,223 bytes.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ORB-001 | PASS | OrbitPanel remains entirely left of PlayerBox; the three preserved TMP slot objects remain active and vertically ordered |
| TC-ORB-002 | PASS | Authored frame is first in render order; procedural rings are disabled; all icon Images are centered, upright, preserve aspect, ignore raycasts, and begin empty |
| Action icon mapping | PASS | Attack resolves its own sprite; `SKL_SHADOW_STRIKE` resolves the Shadow Strike sprite; an unknown skill resolves the generic skill sprite; Guard resolves the shield |
| Slot reset | PASS | Clearing Orbit disables all icon renderers, removes their sprites, and restores the internal empty labels |
| Texture import | PASS | Frame and all four icon PNGs import as single UI sprites with transparency and mipmaps disabled |
| Offscreen visual capture | PASS | Imported scene was rendered at 1600x900 after animation completion; all three symbols appeared within the matching frame squares and no legacy letters were rendered |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, deterministic enemy snapshot, node reset, unchanged Orbit, and unchanged player turn remain intact |
| Full battle/UX regression | PASS | Attack, Shadow Strike cost, Guard, Mana recovery, enemy execution, Convergences, hover preview, rewards, carryover, victory, defeat, restart, and final completion |
| Serialized references and unexpected Console errors | PASS | Existing TMP and battle references remain assigned; no duplicate class, enum, field, or listener was introduced; no unexpected error or exception was accepted |
| Windows Development Build | PASS | Final source and imported icons built for Windows x86_64 with Development enabled; Unity reported Succeeded and 0 errors |
| Interactive standalone playthrough | NOT RUN | Automated Play Mode, offscreen visual rendering, and player build passed; a human-controlled full standalone run is not claimed |
| Complete Standard Run node map | NOT RUN | The prototype still advances through the three existing database battles; selectable 8-10 node routing remains planned |

Report: `Docs/TestResults/OrbitActionIcons-v0.4-PlayMode.xml`.

The final offscreen screenshot and Windows player were generated only in the isolated temporary test project. No capture helper, executable, or build folder was added to the repository.

### Files and Inspector

- Added: `Assets/Project/Art/UI/Orbit/SyngravaOrbitFrame.png`, four action icon PNGs under `Assets/Project/Art/UI/Orbit/Icons/`, their Unity metadata, and `Docs/TestResults/OrbitActionIcons-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Battle.unity now contains `OrbitFrameArtwork`, `OrbitSlot1Icon`, `OrbitSlot2Icon`, and `OrbitSlot3Icon`. All sprite fields and the `SKL_SHADOW_STRIKE` catalog entry are already assigned. No manual Inspector setup is required for the current build.
- The old Orbit TMP objects, procedural `OrbitRings` object, `convergencePreviewText`, battle buttons, reward references, and all earlier serialized fields remain present and connected.
- No package, database schema, locked Base Stat, Analyze rule, reward value, or Convergence rule was changed.

### Known Limitations and Next Milestone

Shadow Strike is the only current skill with an authored dedicated Orbit sprite. Every future skill should add one square transparent sprite and one catalog entry using its stable skill code; until then it deliberately uses the generic skill icon.

The complete Standard Run node graph, save/checkpoint ownership, Heavy Attack and Charge enemy plans, multi-enemy combat, authored reward portraits, Infinite Run, and leaderboard remain planned. Continue with `RunStatController`, then seeded `RunManager` state, before implementing selectable Standard Run nodes.

## Orbit Runtime Visibility and Stage Entrance Follow-up 13 September 2026

### Corrected Behavior

- A live Editor report showed that `OrbitState` reached two actions and the Convergence hover preview reacted correctly, while all three action images remained invisible. The earlier visual capture had invoked `OrbitUIAnimator.SetSlotAction` directly, so it did not prove the real button-to-battle-manager presentation path.
- `BattleManager` now resolves the existing `OrbitUIAnimator` through the preserved slot hierarchy and, if that cached path is unavailable, finds the existing inactive scene component. It does not create a second component. Every Orbit UI update explicitly synchronizes the presentation from the authoritative `OrbitState` list.
- `OrbitUIAnimator` keeps the direct action/skill-code API, retains legacy hidden TMP labels, and adds a self-repair check. If an action remains in `OrbitState` but its `Image`, sprite, or post-animation alpha is unexpectedly hidden, the matching authored icon is restored without changing Orbit gameplay data.
- The three icon objects are direct children of `OrbitPanel`, after `OrbitFrameArtwork` in sibling/render order. Each icon is 118 x 118 while each compatibility slot is 84 x 84, so the rough symbol extends slightly past the inner square and overlays the border. The icon remains centered, upright, aspect-preserving, transparent, and non-raycast.
- Every `StartBattle` plays the Orbit panel entrance after battle chrome becomes active. The complete frame and slots begin 440 UI units to the left, fade in, travel to a subtle 14-unit overshoot, and settle at the authored position in 0.85 seconds using unscaled time. This runs on the first battle and after each completed reward transition into the next database battle.
- Orbit still records only successful Attack, Skill, and Guard actions. Analyze still spends no turn and records no Orbit action. Three valid actions still resolve one Convergence and clear all slots. No damage, mana, reward, snapshot, or node-progression rule changed.

### Verification Results

Unity 6000.6.0f1 compilation and full isolated Play Mode regression: **PASS**. Final suite: **45 passed, 0 failed, 0 skipped** in 29.30 seconds. Windows x86_64 Development Build of MainMenu and Battle: **PASS**, Succeeded with 0 errors, 500 warnings, and output size 224,154,475 bytes.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ORB-001 | PASS | Orbit remains a vertical dropdown entirely to the left of PlayerBox |
| TC-ORB-002 | PASS | All icon objects are direct OrbitPanel overlay children, render after the frame, exceed the 84 x 84 compatibility slots, preserve aspect, ignore raycasts, and start empty |
| TC-ORB-003 | PASS | Two real `basicAttackButton.onClick` calls produce and retain icons in slots 1 and 2 across both enemy turns; an intentionally disabled slot is restored from `OrbitState` |
| TC-ORB-004 | PASS | The full panel starts left of its resting position with reduced alpha, then returns exactly to the authored position and alpha 1 after the stage entrance |
| Runtime visual capture | PASS | A 1600 x 900 render after two real Attack button actions shows two authored Attack icons centered over the first two border squares; the third slot remains empty |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, deterministic enemy snapshot, node reset, unchanged Orbit, and unchanged player turn remain intact |
| Full battle, reward, resource, and Convergence regression | PASS | All pre-existing BattleSnapshotTests passed with no unexpected test error or exception |
| Windows Development Build | PASS | MainMenu and Battle compiled into a Windows x86_64 Development player with zero build errors |
| Human-controlled full standalone run | NOT RUN | Button-path automation, rendered Game view evidence, and player build passed; a full manual standalone playthrough is not claimed |

Reports: `Docs/TestResults/OrbitIconRuntime-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconRuntime-v0.4-GameView.png`.

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added verification artifacts: `Docs/TestResults/OrbitIconRuntime-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconRuntime-v0.4-GameView.png`.
- No new scene class, enum, listener, gameplay field, package, database entry, or `.meta` replacement was introduced. Existing `OrbitPanel`, legacy slot TMP references, icon sprite assignments, frame sprite, `OrbitRings`, battle controls, and Inspector file IDs remain connected.
- No manual Inspector setup is required. Reopening the Battle scene after Unity finishes importing the changed scene/script files is sufficient.

### Known Limitations and Next Milestone

The entrance is a single-panel authored motion and does not yet include particles, sound, controller rumble, or per-slot stagger. Those are presentation polish candidates after the run-state architecture is stable. Shadow Strike remains the only current skill with a dedicated symbol; unknown future skills use the generic skill sprite until their stable code-to-sprite entry is authored.

Continue with `RunStatController`, then seeded `RunManager` ownership of depth, resources, and modifiers. The complete selectable 8-10 node Standard Run map follows that state foundation.

## Orbit Action Icon Centering Follow-up 13 September 2026

### Corrected Behavior

- The authored Orbit frame is intentionally asymmetrical, so the visible centers of its three inner squares do not coincide exactly with the geometric center line of `OrbitPanel`.
- The three action `Image` overlays now use measured local centers from the frame artwork: slot 1 `(10, 101)`, slot 2 `(11, -2)`, and slot 3 `(11, -103)`.
- Attack, skill, and Guard symbols therefore sit in the visual center of their matching empty squares while retaining their 118 x 118 overlay size, upright rotation, aspect ratio, transparency, and render order above `OrbitFrameArtwork`.
- The preserved compatibility TMP objects remain at `(0, 112)`, `(0, 0)`, and `(0, -112)`. Their serialized references and file IDs were not repurposed or moved.
- This adjustment changes presentation coordinates only. Orbit recording, icon mapping, stage entrance animation, Convergence, Analyze, damage, mana, and reward behavior are unchanged.

### Verification Results

- Unity 6000.6.0f1 isolated Play Mode regression: **PASS**, 45 passed, 0 failed, 0 skipped in 29.09 seconds.
- `TC_ORB_002_AuthoredFrameAndActionIconsAreConfigured`: **PASS**. It now asserts the measured X/Y center of every authored icon in addition to overlay order, size, rotation, aspect, raycast, and reset state.
- Isolated 1600 x 900 Game View capture containing Attack, Shadow Strike, and Guard together: **PASS**. All three symbols render in the matching square centers.
- Windows x86_64 Development Build of MainMenu and Battle: **PASS**, Succeeded with 0 errors, 499 warnings, and output size 224,154,474 bytes.
- Human-controlled full standalone playthrough: **NOT RUN**. Automated button-path regression, visual capture, and player build passed.

Reports: `Docs/TestResults/OrbitIconCentering-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconCentering-v0.4-GameView.png`.

### Files and Inspector

- Modified: `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added verification artifacts: `Docs/TestResults/OrbitIconCentering-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconCentering-v0.4-GameView.png`.
- No new class, enum, runtime field, listener, scene object, package, database entry, or asset metadata was added. No manual Inspector setup is required.

## Orbit Runtime Center Enforcement Follow-up 13 September 2026

### Corrected Behavior

- A subsequent live Editor screenshot showed the Skill and Guard images still rendering at the former compatibility-label coordinates. Their actual rendered centers matched `(0, 112)` and `(0, 0)`, leaving them about 9-10 screen pixels left of the authored square centers and leaving the first slot about 10 screen pixels too high.
- `OrbitUIAnimator` now owns the three measured authored slot centers and reapplies them during `Awake` icon preparation and every slot transform reset: `(10, 101)`, `(11, -2)`, and `(11, -103)`.
- This makes the correction effective even when a previously open scene instance, stale serialized value, or runtime UI mutation supplies the old coordinates. Slot animation, icon repair, icon clearing, and action changes all settle back to the same authored centers.
- The correction is presentation-only. The existing scene coordinates, icon references, legacy TMP references, file IDs, action mapping, Orbit state, Convergence, Analyze, rewards, and battle progression remain intact.

### Verification Results

- Full isolated Play Mode regression: **PASS**, 45 passed, 0 failed, 0 skipped in 29.01 seconds.
- Runtime drift recovery: **PASS**. The test forces every action icon to `(0, 0)`, clears the icon presentation, and verifies that all three return to their measured authored centers.
- Live-screenshot scenario capture: **PASS**. A temporary capture helper forces the old slot-1 and slot-2 coordinates, then displays Shadow Strike in slot 1 and Guard in slot 2. The runtime animator corrects both before the 1600 x 900 Game View is rendered.
- Windows x86_64 Development Build: **PASS**, Succeeded with 0 errors, 500 warnings, and output size 224,154,547 bytes.
- Human-controlled standalone playthrough: **NOT RUN**.

Evidence: `Docs/TestResults/OrbitIconCentering-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconRuntimeCentering-v0.4-GameView.png`.

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added verification image: `Docs/TestResults/OrbitIconRuntimeCentering-v0.4-GameView.png`.
- No serialized field, scene object, class, enum, listener, package, database item, or `.meta` file was created or replaced. No manual Inspector assignment is required.

## RunStatController Milestone 13 September 2026

### Implemented Behavior

- Added `RunStatController` as the single calculator for locked Base Stats and temporary active-run modifiers. It is a plain C# object and does not create a scene object or Inspector dependency.
- The controller copies the validated Base Max HP, Base Max Mana, Base Basic Attack, and Base Guard reduction when a run is initialized. These values expose read-only properties and are never changed when rewards stack.
- Effective Max HP is Base Max HP plus all active `MaxHPFlat` modifiers. Vital Core still increases current HP by the exact reward snapshot value after the modifier is committed.
- Effective Basic Attack uses the existing additive reward percentage and the existing `Mathf.RoundToInt` behavior. The prototype Base Basic Attack remains 10.
- Effective Guard reduction uses the existing multiplicative strength formula and remains capped at 90% damage reduction.
- Effective Max Mana currently equals locked Base Max Mana because GDD v0.4 has no Max Mana reward in the implemented pool. Resource caps, Mana UI, and Analyze now read this effective value through the controller.
- Analyze cost is calculated by `RunStatController` from Effective Max Mana with `M - floor(M / 4)`, which is exactly `ceil(0.75 x M)` for non-negative integer Mana. Effective Max Mana 10 therefore costs 8.
- `BattleManager` remains the owner of current HP, current Mana, battle index, turn state, reward flow, and Orbit state. The milestone does not migrate those responsibilities or alter scene references.
- Existing private legacy reward totals and stack fields remain present and synchronized for compatibility with current tests and any existing reflection or Inspector tooling.

### Verification Results

Unity 6000.6.0f1 isolated Play Mode regression: **PASS**, 47 passed, 0 failed, 0 skipped in 28.96 seconds.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-RUN-001 | PASS | `RunModifierCollection` still aggregates, stacks, snapshots, and clears active-run modifiers |
| TC-RUN-002 | PASS | Locked Base Stats remain 100 HP, 10 Mana, 10 Basic Attack, and 50% Guard while Effective Stats consume all three reward types and reset to baseline |
| TC-RUN-003 | PASS | Repeated Guard Strength modifiers cannot raise effective damage reduction above 90% |
| Analyze numeric cases | PASS | Effective Max Mana is the Analyze source; all existing ceiling cases still pass, including Max Mana 10 costing 8 |
| Reward integration | PASS | Attack, Vital Core, and Guard rewards still apply once, stack, persist between battles, and clear on Restart Run |
| Resource integration | PASS | Guard restoration, Unstable Pulse, enemy-turn Mana recovery, restart clamp, and Mana UI use Effective Max Mana |
| Full battle and UX regression | PASS | Enemy snapshot, Analyze, Orbit, Convergence, reward presentation, victory, defeat, retry, stage transition, and icon runtime centering remain green |
| Windows Development Build | PASS | MainMenu and Battle built for Windows x86_64 with 0 errors, 500 warnings, and output size 224,156,838 bytes |
| Human-controlled standalone run | NOT RUN | Automated Play Mode integration and player build passed; a manual full-run playthrough is not claimed |

Report: `Docs/TestResults/RunStatController-v0.4-PlayMode.xml`.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RunStatController.cs`, its `.meta`, and `Docs/TestResults/RunStatController-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- `RunModifierCollection.cs`, `RewardManager.cs`, `RewardDefinition.cs`, the Battle scene, all serialized fields, all existing Inspector references, and existing `.meta` files were preserved without modification in this milestone.
- No manual Inspector setup is required.

### Known Limitation and Next Milestone

The current reward pool has no Max Mana modifier, so Effective Max Mana intentionally equals Base Max Mana. Skill damage remains defined by `SkillData`; Attack Surge continues to affect Basic Attack only, matching its current reward description.

The next milestone is a seeded `RunManager` that owns run depth, seed, current HP/Mana, and the active `RunModifierCollection` while keeping `BattleManager` as the battle executor. After that state foundation is verified, implement the selectable Standard Run node graph with approximately 8-10 nodes and a final boss.

## RunManager Milestone 14 September 2026

### Implemented Behavior

- Added `RunManager` as a plain C# active-run state owner. It does not create a singleton, `MonoBehaviour`, scene object, serialized field, or Inspector dependency.
- `RunManager` now owns the non-zero run seed, current depth, current HP, current Mana, total Embers, active/completed lifecycle flags, and the same `RunModifierCollection` consumed by `RunStatController`.
- Starting a new run clears all temporary modifiers, resets depth to 1 and Embers to 0, restores HP to locked Base Max HP, clamps configured starting Mana to locked Base Max Mana, and marks the run active and incomplete.
- Entering the next battle clamps current HP/Mana against Effective Max values without refilling them. Resource carryover therefore remains intact between nodes.
- Skill and Analyze Mana costs, Guard/Unstable Pulse/enemy-turn Mana restoration, enemy damage, and Vital Core current-HP restoration now mutate `RunManager` through explicit resource methods.
- Victory awards Embers through `RunManager`. Depth advances only after one committed reward selection; repeated clicks cannot advance the run or apply a modifier twice. Final victory marks the run completed, while defeat marks it inactive and incomplete until Restart Run.
- `BattleManager` remains the battle executor and continues to own enemy HP, turn state, EnemyActionPlan, Orbit, Convergence, UI flow, and reward presentation.
- The existing private fields `playerHP`, `playerMana`, `battleNumber`, `totalEmbers`, `runCompleted`, `rewardRunSeed`, and `runModifiers` remain present. They are synchronized compatibility mirrors/references so current reflection tests and existing integration contracts are preserved.
- Reward generation continues to derive a stable per-depth reward seed from the run seed. The next milestone will formalize a portable deterministic run-state snapshot and restore contract; no node-map generation or save system was added here.

### Verification Results

Unity 6000.6.0f1 isolated Play Mode regression: **PASS**, 49 passed, 0 failed, 0 skipped in 29.03 seconds.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-RUN-004 | PASS | RunManager owns the shared modifier collection, non-zero seed, depth, current HP/Mana, Embers, active/completed lifecycle, resource spending/restoration, damage/healing, and guarded depth advancement |
| TC-RUN-005 | PASS | Restart reuses one RunManager instance, clears modifiers and Embers, resets depth/resources/lifecycle, and synchronizes every compatibility field |
| Resource Carryover TC-RES-001 through TC-RES-006 | PASS | New-run initialization, per-node preservation, Vital Core current/max HP, Skill/Convergence Mana, node-entry clamping, and configured starting-Mana clamping remain correct |
| Reward single-commit regression | PASS | A rapid repeated card click applies one modifier and advances exactly one depth |
| Analyze TC-ANL-001 through TC-ANL-007 | PASS | Cost, once-per-node usage, no turn/Orbit mutation, snapshot identity, and per-node reset remain correct through RunManager resources |
| Full battle and UX regression | PASS | Attack, Skill, Guard, enemy turn, victory, defeat, retry, rewards, Orbit, Convergence, hidden intent, tooltip, artwork, icon centering, and stage entrance remain green |
| Windows Development Build | PASS | MainMenu and Battle built for Windows x86_64 with 0 errors, 499 warnings, output size 223,417,836 bytes, and build duration 1 minute 15.47 seconds |
| Human-controlled standalone run | NOT RUN | Automated Play Mode integration and player build passed; a manual full-run playthrough is not claimed |

Report: `Docs/TestResults/RunManager-v0.4-PlayMode.xml`.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RunManager.cs`, its `.meta`, and `Docs/TestResults/RunManager-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- `RunStatController.cs`, `RunModifierCollection.cs`, reward classes, EnemyActionPlan, Orbit classes, the Battle scene, all serialized fields, all existing Inspector references, and existing `.meta` files were preserved without modification in this milestone.
- No manual Inspector or scene setup is required.

### Known Limitation and Next Milestone

The run seed is generated at Restart Run and already stabilizes reward choices per depth, but active-run state is not yet represented by a serializable deterministic snapshot and cannot yet be reconstructed from an explicit seed. Standard Run node selection, deterministic node generation, checkpoint/save, and resume are also not part of this milestone.

The next roadmap item is **Seeded Run State**: define the deterministic, serializable state contract around the existing RunManager seed and lifecycle without implementing the Standard Run Node Map ahead of its own milestone.

## Enemy HP Early-Curve Balance Tuning 14 September 2026

### Implemented Behavior

- Tuned only the `max_hp` values in the tracked seed database for the current three sequential battles. The curve is now Battle 1 `40`, Battle 2 `50`, and Battle 3 `60`, down from `50`, `65`, and `85`.
- This is a data-only balance adjustment. No runtime multiplier, new scaling class, or BattleManager refactor was introduced, so `EnemyData`, `GameDatabase.GetEnemyByBattleIndex`, EnemyActionPlan, and existing Inspector contracts remain unchanged.
- Enemy attack damage remains `12`, `14`, and `17`; Embers rewards remain `10`, `18`, and `30`; and only Battle 3 remains the prototype boss. The HP reduction therefore does not change enemy intent, damage, reward, or victory rules.
- Battle UI continues to display `enemyHP/currentEnemy.MaxHP`, and enemy execution continues to use the already-prepared action snapshot. Both now read the same tuned seed data.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Seed database readback | PASS | Direct SQLite readback confirmed `max_hp` `40/50/60` and preserved damage, rewards, boss flags, codes, and names |
| Balance regression test source | PASS | Added `TC_BAL_001_EnemyMaxHPUsesGentleEarlyCurve` to `BattleSnapshotTests`; it asserts the exact curve and preserved combat metadata |
| Play Mode suite with new balance test | NOT RUN | The first isolated runner exited before tests because the global UPM cache returned `EPERM`; a retry with a temporary writable cache stalled in Unity Licensing and was stopped before producing a report |
| Previous RunManager regression | PASS (historical) | `Docs/TestResults/RunManager-v0.4-PlayMode.xml` records 49 passed, 0 failed, 0 skipped before this balance-only test was added |

### Files and Inspector

- Modified: `Assets/StreamingAssets/Database/game_seed.db`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No scene, serialized field, button listener, public API, `.meta` file, or Inspector reference was changed. No manual Inspector setup is required.

### Known Limitation and Next Milestone

The seed database still contains only the three prototype battles, so this tuning governs the current early sequence. The complete 8–10 node Standard Run and its future node-specific enemy data will need their own balance pass after the node map and seeded run-state contracts exist. At the time of this tuning the roadmap was **Seeded Run State**; that state contract is implemented in the follow-up below.

## Battle Settings Button Follow-up 14 September 2026

### Implemented Behavior

- Added `BattleSettingsUI` to the Battle Canvas. It creates one compact `68 x 68` Settings button at the lower-left anchor with a contrasting purple outline and a centered `42 x 42` procedural gear icon. The reduced footprint follows the compact control scale requested from the Anime Vanguards reference while retaining a comfortable click target.
- The gear is rendered by `GearIconGraphic`, a small custom UI mesh with eight teeth, so the symbol does not depend on a font glyph or an external texture and remains crisp at the CanvasScaler reference resolution.
- `GearIconGraphic` now explicitly requires and configures a `CanvasRenderer` (`cull = false`, `cullTransparentMesh = false`), writes `UIVertex` data directly, and forces its first geometry/material rebuild after sizing. This fixed the reported empty black Settings button on Unity 6; the visible gear was confirmed in the latest user screenshot.
- Clicking the gear opens a dark translucent Settings overlay with a purple-framed card, `SETTINGS` title, a `BATTLE PAUSED` status, and a `RESUME BATTLE` close action. The overlay blocks battle raycasts while visible.
- The Settings card now contains a simple two-column box layout: a category list (`Gameplay`, `Accessibility`, `Graphics`, and `Audio`) on the left, plus a category-specific option list on the right.
- Category buttons rebuild the right-hand option rows without changing BattleManager state. Toggle rows (`ON`/`OFF`) keep their value for the current Settings session; non-toggle rows show clearly marked prototype values such as `1x`, `100%`, and `ANALYZE ONLY`.
- Opening the panel stores the current `Time.timeScale` and pauses gameplay at zero. Closing the panel restores the exact previous scale and returns UI focus to the gear button. Cleanup also restores the scale if the scene is destroyed while the panel is open.
- The scene change adds only the `BattleSettingsUI` component to the existing Canvas. Existing BattleManager fields, button listeners, Orbit references, and Inspector assignments are untouched.
- Unity 6 TextMesh Pro compatibility is preserved by using `textWrappingMode = TextWrappingModes.NoWrap`; the obsolete `enableWordWrapping` property is no longer referenced.
- Runtime initialization is idempotent: `Awake`/`Start` repair an incomplete child hierarchy, and an `AfterSceneLoad` fallback adds the component only when a Battle scene has a `BattleManager` and no existing Settings component. The button is forced active and placed above other Canvas siblings so it remains visible.
- When the overlay opens, the Settings button is promoted above the dimming backdrop as well, keeping the gear readable and allowing the same control to close the panel; `RESUME BATTLE` remains available as the primary close action.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Scene reference audit | PASS | `BattleSettingsUI` points to the new script GUID on the existing Canvas; no BattleManager reference changed |
| Static UI contract | PASS | `TC_SET_001_SettingsButtonOpensAndRestoresBattlePause` asserts one gear child, a CanvasRenderer with opaque graphic colour, lower-left anchors, comfortable size, one-click open, pause, toggle close, time-scale restoration, and top-sibling layering while the overlay is open |
| Settings category/list contract | PASS (source) | `TC_SET_002_SettingsPanelShowsCategoryListAndOptionRows` asserts four category buttons, option labels, and category switching to the Accessibility list |
| TMP API compatibility scan | PASS | Repository search finds no `enableWordWrapping` usage; the Settings label uses the Unity 6 `textWrappingMode` API |
| Runtime visibility fallback | PASS (user verified) | The latest Play Mode screenshot shows the lower-left button, purple hover border, and the visible gear icon after the CanvasRenderer/mesh rebuild patch |
| Play Mode with Settings test | PASS (user verified) | The provided screenshot confirms the overlay opens with `SETTINGS`, `BATTLE PAUSED`, and `RESUME BATTLE`; the visible gear was confirmed before this category-layout change |
| Manual visual review of category layout | NOT RUN | The new two-column list and category switching require a fresh Play Mode screenshot after Unity recompiles the runtime script |

### Files and Inspector

- Added: `Assets/Project/Scripts/UI/BattleSettingsUI.cs` and its `.meta` file.
- Modified: `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No manual Inspector assignment is required beyond the serialized Canvas component already stored in the scene. The script builds its child UI at runtime.

### Known Limitation and Next Milestone

The current panel is a safe entry point and pause shell. The category rows are a presentation layer: their values are not yet connected to audio mixers, display settings, input rebinding, animation speed, or BattleManager behavior. The latest resized button and the new category layout require a fresh live visual recheck after Unity recompiles the runtime script. The next gameplay milestone is **Standard Run Node Map** after the Seeded Run State follow-up.

## Seeded Run State Milestone 14 September 2026

### Implemented Behavior

- Added `SeededRunState`, a serializable state contract for the existing `RunManager` lifecycle. It records the schema version, ruleset version, run seed, depth, current HP, current Mana, total Embers, active/completed flags, and active-run modifiers.
- Added `SeededRunModifierState` as the serializable value object for each immutable runtime modifier. Modifier records are copied into the state and sorted by `SourceCode`, so dictionary iteration order cannot change a checkpoint identity.
- Added `RunManager.CaptureState()` and `RunManager.RestoreState(...)`. Capture never exposes the live modifier records; restore validates the schema, ruleset, seed, depth, lifecycle, resource bounds, modifier values, and duplicate source codes before replacing state.
- Effective Max HP and Max Mana remain derived by `RunStatController` and are supplied to restore as caps. The snapshot therefore does not create a second stat calculator or alter locked Base Stats.
- Added `RunModifierCollection.CreateStateSnapshot()` and `RestoreStateSnapshot(...)`. Restore builds a complete replacement collection first, so invalid serialized data cannot partially mutate the active run.
- Existing `RunManager` methods, `BattleManager` serialized fields, scene references, reward flow, resource carryover, and Inspector assignments remain intact. No scene setup is required for this state-only milestone.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Repository and duplicate-definition audit | PASS | Git root and required context were read; the new state classes have unique names and no existing class or enum was duplicated |
| State contract source audit | PASS | `SeededRunState` fields cover the current `RunManager` state and modifier collection without duplicating derived stats |
| Test assembly reference audit | PASS (source) | The first Console compile exposed direct production-type references in the test asmdef; `TC_RUN_006` and `TC_RUN_007` now use the existing reflection pattern and no longer require an `Assembly-CSharp` reference |
| Deterministic JSON round-trip test source | PASS (source) | `TC_RUN_006_SeededRunStateRoundTripsDeterministically` covers serialization, restore, modifier isolation, and different seed identity |
| Invalid restore guard test source | PASS (source) | `TC_RUN_007_SeededRunStateRejectsInvalidDataWithoutMutation` covers invalid seed rejection and no partial mutation |
| Unity compile after this milestone | NOT RUN | Unity compiler execution was unavailable in this session; no compile result is claimed for the new source |
| Play Mode `TC_RUN_006` and `TC_RUN_007` | NOT RUN | Unity Editor/Test Runner was not available after the code change |

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/SeededRunState.cs` and its `.meta` file.
- Modified: `Assets/Project/Scripts/Battle/RunManager.cs`, `Assets/Project/Scripts/Battle/RunModifierCollection.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No Inspector or scene assignment is required. The state contract is ready for a future `SaveService` or checkpoint repository to serialize, but no file I/O was added in this milestone.

### Known Limitation and Next Milestone

`SeededRunState` is currently an in-memory and serialization-ready contract. `BattleManager` does not yet save it to disk, generate a selectable node graph, or restore it through a Continue button. The next smallest implementation is **Standard Run Node Map**, backed by a deterministic node generator that consumes the run seed and keeps the final boss fixed at the end.

## Standard Run Node Map Milestone 14 September 2026

### Implemented Behavior

- Added `StandardRunNodeMap` as a plain serializable route model with public `RunSeed` and ordered `Nodes` fields. Each node stores a one-based `Depth`, `StandardRunNodeType`, stable `Code`, and `IsFinal` flag.
- Added `StandardRunNodeGenerator` with a small deterministic xorshift sequence. The same non-zero seed and node count always produce the same node order; the generator does not depend on Unity scene state or dictionary iteration order.
- The generator accepts 8, 9, or 10 nodes and defaults to the GDD target of 10. The first node is always `NormalBattle`, exactly one `EliteBattle` and one final `Boss` are generated, at least one `Rest` appears before the Boss, and no two Elite nodes can be adjacent. The route includes two or three `RandomEvent` nodes and at most one `Merchant`.
- `StandardRunNodeMap.Validate()` is the single structural guard for node count, depth ordering, unique codes, final-node flag, allowed node types, battle/event quotas, Elite/Boss counts, and the Rest-before-Boss rule.
- `GetNodeAtDepth()` and `TryGetNodeAtDepth()` expose the one-based depth contract used by `RunManager` while keeping invalid access explicit.
- No Battle scene, serialized field, Inspector reference, database schema, or `BattleManager` flow was changed. The current database still contains only three sequential battle encounters, so routing those encounters through all Standard Run node types is intentionally a later integration step.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Repository/context/duplicate audit | PASS | Git root, `Assets`, `Packages`, `ProjectSettings`, AGENTS, PROJECT_CONTEXT, and both GDD copies were read; no existing node type or map class was found before addition |
| Production source compile | PASS | `Add-Type` compiled `StandardRunNodeMap.cs` with no compiler errors |
| Deterministic route and rule sweep | PASS | 3,000 generated combinations across seeds `-500..500` (zero skipped) and node counts 8–10 validated successfully; a second 250-case random-seed sweep also passed |
| Invalid input/access guards | PASS | Zero seed, node counts 7/11, and out-of-range depth cases were rejected with the expected exceptions/results |
| Mutation guards | PASS | Valid lookup works, invalid lookup returns false/throws as designed, and corrupted depth or duplicate node codes are rejected by `Validate()` |
| Seed variation | PASS | Repeated seeds produced identical route keys; seeds 123 and 321 produced different route keys |
| Play Mode test source | PASS (source) | Added `TC_NODE_001` through `TC_NODE_005` for shape, determinism, 8–10 support, invalid inputs, and JSON round-trip using the existing reflection pattern |
| Unity compile and Play Mode execution | NOT RUN | The isolated runner reached Unity licensing initialization but timed out and produced no test XML; it was stopped before changing the live project |

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/StandardRunNodeMap.cs` and its `.meta` file.
- Modified: `Assets/Tests/PlayMode/BattleSnapshotTests.cs` and this context file.
- No scene or Inspector setup is required for this data-only milestone. Existing `BattleManager` serialized fields, button listeners, Orbit references, database files, and `.meta` GUIDs remain untouched.

### Known Limitation and Next Milestone

The map can now be generated, validated, queried, and serialized, but it is not yet displayed as a selectable UI map and does not choose enemy/event payloads. `RunManager.Depth` and the current prototype `BattleManager` still advance through the three database battle indexes. The next smallest step is to connect a generated map to `RunManager` and add a guarded node transition that routes Battle, Rest, Event, Elite, Merchant, and final Boss nodes without refilling carried resources.

## Standard Run Map Visual Direction Reference 14 September 2026

### Reference and Design Goal

- The user supplied an “Odyssey Route Atlas” screenshot as visual inspiration for the future SYNGRAVA Run Map: a modal route atlas with branching, connected nodes, a legend, a selected-node detail panel, and optional zoom controls.
- Treat the screenshot as hierarchy and usability inspiration only. Do not copy its artwork, typography, logos, exact layout, icons, or palette.
- The SYNGRAVA target is an original black-purple-white route map with a dark panel, a controlled purple border glow, branching connected routes, readable node icons, clear legend labels, a player position marker, and a detail panel for the selected node.
- Readability takes priority over decoration: route lines must remain distinct from ornaments, reachable choices must be obvious, and the final Boss route must remain easy to identify.
- The first implementation should be a functional map shell using existing palette values and procedural or placeholder UI. Authored art and polish can follow once deterministic map logic and node-transition tests are connected.
- Optional zoom controls and a right-side legend/detail panel are presentation goals taken from the reference, not a requirement to reproduce every control shown there.

### Planned Map Components

- Map modal/background panel and an original black-purple-white frame.
- Route connectors for inactive/locked, reachable/highlighted, travelled/completed, and current paths.
- Node icon set for Start/Current, Normal Battle, Elite Battle, Random Event, Rest, Merchant, and Boss.
- State overlays for locked, reachable, selected, completed, and current nodes. A tint, outline, or material can provide these states without duplicating every texture.
- Selection feedback with a purple outline/glow, hover/focus state, and selected-node detail panel.
- Header/title and close control; optional zoom-in, zoom-out, and reset controls.
- Legend icons and labels that match the node icon set.
- Small SYNGRAVA eye, orbit, and shard ornaments used sparingly around the frame and selection accents.
- Detail-panel backing, type-icon slot, title, description, reward, and requirement placeholders.
- Optional polish such as node pulse, route reveal, a travelling marker, and lightweight VFX. These must not block the functional map.
- Use TextMesh Pro for labels. Do not draw a separate font unless an original or properly licensed typeface is selected later.

### Asset Brief for Production

Minimum authored or procedural assets:

1. One transparent 9-slice or tileable map-frame texture.
2. One dark map-panel/background texture, or a procedural panel with the approved SYNGRAVA palette.
3. Node icons for Normal Battle, Elite Battle, Random Event, Rest, Merchant, Boss, and Start/Current.
4. State overlays for locked, reachable, selected, completed, and current; these may be UI materials, shader effects, outlines, or procedural shapes.
5. Route/connector textures or vector/procedural dashed lines for inactive, reachable, and travelled states.
6. A matching legend icon set.
7. One close/exit icon and optional zoom icons.
8. Small SYNGRAVA eye/orbit/shard ornaments for frame corners and selection accents.
9. Optional detail-panel ornament/background.

Recommended production format:

- Keep layered PSD/ORA/SVG sources where possible and export transparent PNG/SVG at approximately 2x the intended reference size.
- Use square canvases and consistent padding for node icons so their hit areas and visual centers line up.
- Make the outer frame a 9-slice asset when it must stretch across resolutions.
- Keep black dominant, purple as the accent/glow, and white for readable content and active highlights.
- Avoid copying the reference game's skull art, logo, exact typography, or red palette.

### Acceptance Direction for the Future RunMap

- The same `RunSeed` produces the same map and route connectivity.
- Only reachable nodes are selectable; travelled and locked nodes reject clicks.
- Player position updates after a node is resolved.
- The selected node's type, title, description, and detail panel match its data.
- The final Boss is clearly represented and remains the last node.
- Reopening the map does not regenerate a different route.
- The map remains inside safe Canvas bounds at the project's responsive 16:9 design resolution.

## Run Node Lifecycle Integration Milestone 19 September 2026

### Implemented Behavior

- `RunManager` now creates and owns one `StandardRunNodeMap` when a run starts. The existing `StartNewRun(...)` API remains intact and creates the approved default ten-node route; `StartNewRunWithNodeCount(...)` supports the generator's validated 8–10 node range without adding an ambiguous overload.
- `NodeMap`, `CurrentNode`, and `HasNextNode` expose the active deterministic route to the future RunMap UI and encounter router. The route seed always matches `RunManager.RunSeed`.
- `TryResolveCurrentNode(expectedType, expectedNodeCode)` is the guarded node transaction. It accepts only the current node's exact type and stable code, rejects stale callbacks and wrong node handlers without changing depth, advances one node at most, and completes the run when the final Boss node is resolved.
- Repeating a callback from a node that was already resolved cannot resolve the next node because its stable code no longer matches. Repeating the final Boss callback is also rejected after the run is complete.
- Resolving a node changes only run position and lifecycle. Current HP, current Mana, Embers, and the modifier collection are not refilled, reset, or rewritten by node movement.
- The legacy `TryAdvanceDepth()` API remains available for existing callers and now uses the same guarded current-node path. It refuses to advance beyond the final node.
- `SeededRunState` schema is now version 2 and records `NodeCount`. Capture stores the map size; restore validates the 8–10 node range and depth bounds, regenerates the exact route from `RunSeed + NodeCount`, then restores runtime state.
- Route generation and checkpoint validation occur before mutable runtime state is replaced. A corrupt node count or out-of-range depth therefore cannot partially replace resources or modifiers.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Production model compile | PASS | PowerShell `Add-Type` compiled `RewardDefinition`, `RunModifierCollection`, `StandardRunNodeMap`, `SeededRunState`, and `RunManager` together with no compiler errors |
| Deterministic node lifecycle sweep | PASS | 1,500 combinations covering seeds 1–500 and node counts 8, 9, and 10 completed through the guarded API; every run ended on its Boss and preserved HP/Mana |
| Invalid and duplicate transaction guards | PASS | Every sweep case rejected a wrong node type, foreign code, repeated previous-node callback, and repeated completed-Boss callback without extra advancement |
| Snapshot restore and corruption sweep | PASS | 250 additional seeds restored the same route, depth, HP, and Mana; an out-of-range checkpoint depth was rejected without mutating the target run |
| Play Mode test source compile | PASS | Added `TC_NODE_006_RunManagerOwnsAndGuardsNodeProgression` and `TC_NODE_007_SeededStateRestoresTheSameNodeMap`; the Unity-generated test project compiled with 0 errors and 0 warnings using temporary MSBuild output |
| Unity isolated Play Mode execution | NOT RUN | Two isolated Unity 6000.6.0f1 attempts stopped before compilation because the Unity Licensing Client repeatedly lost its IPC connection; no test XML was produced and no Play Mode PASS is claimed |

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/RunManager.cs`, `Assets/Project/Scripts/Battle/SeededRunState.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No Battle scene object, serialized BattleManager field, button listener, database row, art asset, public battle API, or `.meta` GUID was changed.
- No Inspector or Hierarchy assignment is required for this lifecycle milestone.

### Known Limitation and Next Milestone

The deterministic map is now part of the run lifecycle, but the current `BattleManager` prototype still presents its original three sequential database battles and does not dispatch `NormalBattle`, `EliteBattle`, `RandomEvent`, `Rest`, `Merchant`, or `Boss` from `CurrentNode`. The map also remains an ordered route model rather than a visible branching selection screen.

The next smallest milestone is a **RunMap encounter router and functional placeholder UI**: display the generated route, mark current/reachable/completed states, permit only valid node selection, and dispatch each selected node type to its appropriate scene or placeholder handler. That integration must preserve resource carryover and use `TryResolveCurrentNode` as the only commit point.

## RunMap Encounter Router and Functional UI Milestone 19 September 2026

### Implemented Behavior

- Added `StandardRunMapUI`, a runtime UI component attached to the existing Battle Canvas without adding a serialized `BattleManager` field or changing the Battle scene. It displays the complete 8–10 node route, route connectors, progress, a legend, selected-node details, and one contextual enter button.
- The current route is shown after `RestartRun()` and after every non-final battle reward. Battle command and Orbit panels are hidden while the map is open and restored only after a valid battle node is entered.
- Completed, current/reachable, and locked nodes have separate presentation states. Only the exact current-depth node is interactable. Locked and completed buttons reject selection.
- The UI callback passes the node's stable code to `BattleManager`. The router validates that the map is open and that the code still matches `RunManager.CurrentNode`; foreign, stale, repeated, and post-close callbacks cannot advance depth or restart the encounter.
- `NormalBattle`, `EliteBattle`, and `Boss` dispatch to the existing Battle scene flow. Until dedicated encounter pools exist, Normal nodes alternate the two non-boss database prototypes, Elite uses the second prototype, and the final Boss uses database battle index 3. This mapping does not alter database rows, enemy stats, or serialized scene references.
- A non-final battle opens exactly one reward selection. Committing the reward resolves that exact battle node through `RunManager.TryResolveCurrentNode(...)` and returns to the Run Map. Repeated reward clicks remain guarded.
- `RandomEvent`, `Rest`, and `Merchant` use explicit functional placeholders. Selecting one resolves the exact current node and returns to the next map position without changing current HP, current Mana, Embers, Orbit, or run modifiers. No unapproved healing, price, event outcome, or balance value was invented.
- `RUN COMPLETE` is now reached only after the route's final Boss node is defeated and committed. Exhausting the three current database encounter rows no longer ends the run early.
- A new run resets Analyze usage, Guard state, enemy-plan snapshot, and Orbit before the map opens. Entering each battle still creates a fresh deterministic `EnemyActionPlan`, resets Analyze for that node, and preserves carried HP/Mana.

### Route Node Map Visual Production Milestone

The functional shell deliberately uses procedural rectangles, TextMesh Pro labels, outlines, and the approved black-purple-white palette. Authored artwork is a separate visual-production milestone so art replacement cannot destabilize routing or gameplay.

Planned visual deliverables:

1. One original transparent 9-slice outer frame and one optional inner/detail frame in the SYNGRAVA rough eye/orbit/shard language.
2. One dark map backing texture with subtle, low-contrast Singularity marks that does not compete with route lines.
3. Centered, consistently padded icons for Start/Current, Normal Battle, Elite Battle, Random Event, Rest, Merchant, and Boss.
4. Reusable state overlays or UI materials for locked, reachable, selected, current, and completed nodes.
5. Connector assets or materials for inactive, reachable, and travelled routes, always rendered behind node buttons.
6. A player-position marker, matching legend icons, selection marker, close icon, and optional zoom-in/zoom-out/reset icons.
7. Optional frame-corner eyes, shards, and orbit scratches used sparingly so text and route readability remain dominant.
8. Layered PSD/ORA/SVG source files plus transparent PNG/SVG exports at approximately 2x display size. Node icons must use identical square canvases and visual-center padding; stretchable frames must be prepared for Unity 9-slicing.

Visual acceptance requirements:

- Black remains dominant, purple communicates route state and selection, and white is reserved for active/readable information.
- Every node state must remain distinguishable without pointer hover and at the project's 16:9 reference resolution.
- Node icons must remain centered inside their hit areas, connectors must not overlap labels, the final Boss must be immediately recognizable, and the entire route/detail panel must stay inside Canvas safe bounds.
- The supplied route-atlas screenshot remains hierarchy and usability inspiration only. Its skull art, logo, typography, exact layout, palette, and game-specific symbols must not be copied.
- Authored visual replacement must preserve the existing routing API, node codes, selection guards, and Play Mode tests.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Production source compile | PASS | Unity-generated `Assembly-CSharp.csproj` built with 0 errors; the only warning is the pre-existing obsolete `FindFirstObjectByType` call elsewhere in `BattleManager` |
| Play Mode test source compile | PASS | `Syngrava.Battle.PlayModeTests.csproj` built with 0 errors and 0 warnings using temporary MSBuild output |
| Initial isolated Unity run | FAIL, FIXED | 63/64 passed; the failure correctly exposed that Analyze/Orbit were reset on battle entry instead of at the new-run boundary |
| Final isolated Unity Play Mode run | PASS | 64/64 tests passed, 0 failed, 0 skipped in Unity 6000.6.0f1; execution used an isolated project and product name so the live runtime database was not touched |
| `TC_MAP_001` | PASS | New run opens the map, creates one button per route node, exposes matching details, and leaves exactly the current node reachable |
| `TC_MAP_002` | PASS | Valid selection starts one battle; foreign, stale, and repeated post-close callbacks do not advance or recreate the plan |
| `TC_MAP_003` | PASS | Rest/Event/Merchant placeholder resolution advances once and preserves HP, Mana, Embers, and modifier ownership |
| Full-run regression | PASS | Defeat/restart resets run state, all route nodes can be traversed, rewards return to the map, and completion occurs only after the final Boss |

### Files and Inspector

- Added: `Assets/Project/Scripts/UI/StandardRunMapUI.cs` and `Assets/Project/Scripts/UI/StandardRunMapUI.cs.meta`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No scene, database, package, existing `.meta` GUID, serialized `BattleManager` field, public API, or Inspector reference was removed or renamed. The existing Battle Canvas receives the map component at runtime, so no manual scene assignment is required.

### Known Limitations and Next Milestone

- The current data model is a deterministic ordered route displayed as a two-row path. True branching and multiple reachable choices require a later connectivity model and must not be faked only in UI.
- Rest, Random Event, Merchant, and dedicated Elite content are placeholders with no effect. Their full systems remain separate milestones.
- Encounter routing reuses the three current database prototypes. Dedicated seeded encounter pools, enemy variants, and node-specific rewards are not part of this milestone.
- The current Run Map uses procedural placeholder visuals and has no authored route-map textures, icons, zoom controls, travel animation, or branching animation yet. The visual-production brief above is the approved preparation target.
- Following the established feature order, the next technical milestone is **Checkpoint and Save Active Run**, using `SeededRunState` schema 2 and the current node code/depth as the restore boundary. Authored Run Map visuals can proceed in parallel after the required original assets are available and approved.
