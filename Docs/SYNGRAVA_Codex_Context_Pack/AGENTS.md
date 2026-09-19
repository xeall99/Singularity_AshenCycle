# SYNGRAVA Project Instructions

These instructions apply to the entire repository. Read this file before changing code, scenes, prefabs, database files, or documentation.

## Required Context

Before proposing or implementing a change:

1. Read `Docs/PROJECT_CONTEXT.md` completely.
2. Treat `Docs/GDD_SYNGRAVA_UKK_v0.4.docx` as the full design and UKK planning document.
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
2. Inspect the newest relevant scripts and search for duplicate class or enum names.
3. Describe the smallest safe implementation and its acceptance criteria.
4. Edit only the required files.
5. Perform available static checks and search for broken references.
6. Ask the user to let Unity compile, then collect the complete Console error and reproduction steps if it fails.
7. Provide exact Inspector assignments or Hierarchy changes.
8. Update `Docs/PROJECT_CONTEXT.md` and the GDD status only after the behavior is accepted.
9. Recommend a small Git commit after the Unity test passes. Do not commit, push, publish, or overwrite user work unless explicitly requested.

## Completion Report

Every implementation response must state:

- files changed;
- behavior added or corrected;
- Inspector or Hierarchy work still required;
- checks performed;
- checks that still require Unity Editor or a Windows build;
- known limitations and the next smallest task.

