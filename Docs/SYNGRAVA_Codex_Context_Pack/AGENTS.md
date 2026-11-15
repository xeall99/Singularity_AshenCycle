# SYNGRAVA Project Instructions

These instructions apply to the entire repository. Read this file before changing code, scenes, prefabs, database files, or documentation.

## Required Context

Before proposing or implementing a change:

1. Read `Docs/PROJECT_CONTEXT.md` completely.
2. Treat `Docs/GDD_SYNGRAVA_UKK_v0.5.docx` as the full design and UKK planning document.
3. Inspect the current files involved in the requested feature. Do not reconstruct a file from an old chat snippet when a newer file exists in the repository.
4. Run `git status` and preserve every unrelated user change.
5. State which files will change and what behavior will be tested.

The GDD and context files provide durable project memory. They do not train or modify the model. Update them after the user accepts a design change so future chats receive the same decisions.

## Authority Order

Use this order when information conflicts:

1. The user's newest explicit instruction.
2. This `AGENTS.md` file.
3. `Docs/PROJECT_CONTEXT.md`.
4. The latest GDD decision log.
5. Current source code and scene configuration as evidence of implemented behavior.

If desired design and current code differ, report the difference. Do not silently rewrite the project to match an assumption.

## Project Identity

- Active title: SYNGRAVA.
- Previous working title: Singularity Ashen Cycle.
- Genre: single-player turn-based dark-fantasy roguelite RPG.
- Engine: Unity 6.6 `6000.6.0f1`.
- Initial render setup: Universal 2D with a staged path toward a 2.5D presentation.
- Target: Windows PC and an SMK RPL UKK demonstration.
- Developer scope: one solo developer with Standard Run as the mandatory release target.

## Non-Negotiable Game Rules

- The player controls one active Warden, not a three-character party.
- Warden Base Stats are locked. Permanent progression unlocks options and information, not raw starting-stat advantages.
- Numerical upgrades are Temporary Run Stats and reset when a run is Cleared, Defeated, or Abandoned.
- Standard Run contains approximately 8 to 10 nodes and ends with a boss.
- Infinite Run may stack temporary upgrades without a fixed stack cap during that active run. The entire temporary build resets when that Infinite Run ends.
- Singularity Orbit records only successful Attack, Skill, and Guard actions. Analyze never enters Orbit.
- Three valid Orbit actions trigger exactly one Convergence, after which the three gameplay slots reset.
- Enemy intent is hidden by default.
- Analyze costs `ceil(0.75 * EffectiveMaxMana)`, can be used once per node, reveals the already-prepared enemy action, and does not consume the player's turn.
- A successful Analyze leaves the player free to choose Attack, Skill, or Guard.
- Reward selection opens automatically after a non-final victory. The player must not press a separate Choose Reward button.
- The Convergence preview appears only while Attack, Skill, or Guard is hovered or focused when two Orbit slots are filled.

## Current Implementation Strategy

Work incrementally. Preserve existing Inspector references and serialized field names unless a migration is explicitly planned. In particular:

- Do not create duplicate definitions of `PlayerActionType`, `OrbitState`, `OrbitResolver`, `ConvergenceType`, or `ConvergenceResult`.
- Keep existing public APIs used by `BattleManager`, `BattleVisuals`, `GameDatabase`, `SkillData`, and `EnemyData` unless all callers are updated in the same change.
- Keep `.meta` files with their Unity assets. Never regenerate or delete them casually.
- Do not edit generated Unity folders: `Library`, `Temp`, `Logs`, or `obj`.
- Do not add a Unity package, alter the SQLite schema, rename assets, or move scenes unless the task requires it and the user approves the scope.
- Use `Time.unscaledDeltaTime` for modal UI transitions that must continue while gameplay is paused.
- Block repeated clicks and battle input while actions, victory transitions, or reward selection are resolving.

## Current Feature Priority

The active milestone is Battle UX version 0.4:

1. Hidden enemy plan with stable pre-rolled execution data.
2. Analyze using the approved option A behavior.
3. Hover-only Convergence preview above the relevant command button.
4. Automatic three-card reward selection with black transparent dimming.
5. Smooth card entrance and an animated hover border.
6. Regression testing for Orbit, rewards, mana, victory, defeat, and Inspector references.

Infinite generation, save and resume, local leaderboard, Ranked mode, and global leaderboard must not delay the Standard Run milestone.

## Required Workflow

For each task:

1. Confirm the repository root contains `Assets`, `Packages`, and `ProjectSettings`.
2. Inspect the newest relevant scripts, project context, latest design/status notes, and search for duplicate types before deciding scope.
3. Before every feature or addition, do a focused design pass: research relevant current primary/authoritative online sources when useful or explicitly requested, inspect the repository and related implementation, identify constraints and edge cases, then write a complete feature design/acceptance plan before implementation. Cite sources in the plan. Do not treat a research claim as a project rule without reconciling it with the GDD and owner decisions.
4. If the user asked to proceed with the feature, implement it after the plan without pausing for routine approval. Preserve all unrelated and user-owned changes.
5. Run an available automated Unity Play Mode baseline before implementation. After implementation, run the full relevant Play Mode suite **three consecutive times**, using isolated copies/profiles and separate result logs/XML. A failed or blocked attempt does not count; fix issues and restart the consecutive pass sequence. Do not substitute manual playtesting or claim a test pass without a passing result file.
6. If Unity automation is blocked by licensing, sandbox, or environment limits, make reasonable isolated/out-of-sandbox runner attempts where available, preserve logs, and report the blocker honestly. Continue with static verification, but mark the feature as not fully verified until all three automated passes succeed.
7. Perform static checks and search for broken references. Provide exact Inspector/Hierarchy work only if the implementation actually requires it.
8. Update `Docs/PROJECT_CONTEXT.md`, the design/status notes, and a focused milestone report after implementation and verification. Keep intended behavior distinct from verified behavior.
9. Do not commit, push, publish, reset, clean, or overwrite user work unless explicitly requested.

## Completion Report

Every implementation response must state:

- files changed;
- behavior added or corrected;
- Inspector or Hierarchy work still required;
- checks performed;
- checks that still require Unity Editor or a Windows build;
- known limitations and the next smallest task.

The owner specifically requests a complete design before each feature, prior automated baseline checking, and three consecutive automated Unity Play Mode passes after implementation; manual owner testing is not a prerequisite when those passes can be run.

