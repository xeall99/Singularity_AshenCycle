# Claude Code Notes — SYNGRAVA

Use this file as a short entry point for work in this Unity repository. The checked-in project, its instructions, and current source are authoritative; chat summaries are not.

## Read before work

1. Read `Docs/SYNGRAVA_Codex_Context_Pack/AGENTS.md` and `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md` completely.
2. Check the latest GDD at `Docs/GDD_SYNGRAVA_UKK_v0.5.docx` and relevant milestone report(s) in `Docs/TestResults/`. Treat the GDD and project context as design history; verify implementation in current source.
3. Run `git status --short --branch`, inspect the relevant source/assets/tests, and preserve existing staged, unstaged, and untracked changes. Do not assume that local changes belong to you.
4. State briefly what you understand, the smallest task you will do, and how you will verify it. Continue through implementation without waiting for routine technical decisions.

## Project guardrails

- Project: **SYNGRAVA**, a single-player dark-fantasy roguelite RPG made with Unity 6.6 (`6000.6.0f1`), targeting Windows PC and an SMK RPL UKK demonstration.
- Follow the newest user instruction first, then repository `AGENTS.md`, `PROJECT_CONTEXT.md`, and the latest GDD decision log. When they conflict, inspect code and report the conflict instead of silently assuming.
- Preserve the one-Warden design, locked base stats, temporary run upgrades, 8–10-node Standard Run with a final Boss, successful-action-only Singularity Orbit, hidden enemy intent by default, and the approved Analyze/Convergence/reward rules described in project context.
- Keep existing Unity `.meta` files, serialized field names, Inspector references, public APIs, and save compatibility. Avoid duplicate core Orbit/action types. Do not casually change packages, SQLite schema, scenes, or generated Unity folders (`Library`, `Temp`, `Logs`, `obj`).
- Maintain the established SYNGRAVA visual language: black, violet, and white fractured-paper/crystal styling. Reuse approved Figma decisions and existing in-game assets where applicable; do not claim Canva/Figma work unless you actually performed it.
- Make focused changes. Do not commit, push, publish, reset, clean, or overwrite user work unless explicitly asked. In particular, never run `git add -A` blindly in this project: inspect the exact staged file list first.

## Efficient implementation and verification

- Owner workflow: before every feature/addition, research and inspect first, write a complete focused design/acceptance plan, and only then implement. Run an automated Play Mode baseline before implementation when available, then run the full relevant Play Mode suite three consecutive times afterward using isolated copies and separate reports. Failed/blocked attempts do not count. Manual owner testing is not required when automation is available. If the Unity runner is blocked, preserve logs, try the reasonable isolated runner path, and report that three-pass verification is incomplete rather than claiming success.
- Read only the relevant source paths after the required context pass; search for existing types/services before creating new ones.
- Keep code consistent with nearby Unity/C# patterns. Add or update meaningful tests for behavior changes when Unity test tooling is available and the task calls for verification.
- Prefer an isolated Unity project/test copy for Play Mode tests when production initialization can mutate databases or saves. Never overwrite the user's save data. If Unity is unavailable, state that clearly and label code review/static checks as such; never describe them as Play Mode tests.
- If a check fails, identify the cause, fix it, and rerun the necessary checks. Record the actual command/result and any remaining limitation.
- Update `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md` and add a focused report under `Docs/TestResults/` when a milestone is completed. Update the GDD only when its design/status record needs changing, and preserve the original document structure.

## Handoff and report

When continuing another agent's work, first ask for or inspect its actual diff and compare it with this repo. Do not assume that files described as being in a separate sandbox exist here. Reconcile duplicated implementations with the current code before merging anything.

At completion, report the files changed, behavior delivered, tests/checks actually run, any Unity Editor/manual/build verification still needed, remaining limitations, and the next milestone. Keep the report concise and never claim success without evidence.
