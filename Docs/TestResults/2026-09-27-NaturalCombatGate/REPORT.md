# Standard Run natural combat verification — 27 September 2026

## Scope

Follow-up to the 26 September Standard Run gate. The earlier `TC_E2E_001` proves scene/save/result integration but calls `EndBattle(true)` to skip encounter execution. This follow-up adds gameplay coverage using normal command and reward button listeners. It does not replace the required three human-controlled Windows playthroughs.

Production gameplay, balance, scenes, packages, Inspector references and save formats were not changed. Existing staged, unstaged and untracked work was preserved.

## New coverage

- `TC_E2E_003_ThreeExpeditionsUseRealCombatAndCommitTerminalResults`: starts three fresh expeditions, selects connected nodes through map buttons, uses Skill when available and Attack otherwise, waits for real enemy turns/cut-ins and automatic reward entrance, and selects displayed rewards. It prefers Max HP rewards, takes the second Event choice and leaves Merchant. No direct victory call, enemy HP override, resource injection or node advancement is used inside the test. Each run must reach a valid terminal result within bounded time/actions. Both Clear and Defeat are valid outcomes; the actual sampled results are recorded below rather than asserting every seed is winnable with this simple policy.
- Each terminal result must open Summary, commit exactly one History record, finish the Echo transaction, remove the resumable checkpoint, and report exactly the accepted action count. Every subsequent run starts at depth 1 with an empty modifier collection.
- `TC_E2E_004_NaturalDefeatArchivesOnceAndNewRunResetsState`: starts a fresh first battle and uses Guard until real enemy damage reduces HP to zero. Checks defeat/summary/history/checkpoint behavior and a fresh run's new identity, full baseline HP, zero action count and empty modifiers.

These are automated Unity Play Mode tests using `Button.onClick`, with time scale accelerated by the existing fixture. They are not physical mouse/controller coverage, human strategy evaluation, or standalone combat tests. Initial run setup calls the established RestartRun method; menu navigation remains covered by existing E2E tests.

## Results

Focused natural-combat test: **1/1 PASS**, 0 failed, 0 skipped, **32.8198308 seconds**. Evidence: `focused-natural-combat-results.xml`.

| Focused sample | Seed | Outcome | Final depth | Player actions | Final HP |
| --- | ---: | --- | ---: | ---: | ---: |
| 1 | 1019079324 | Clear | 10 | 18 | 65 |
| 2 | -1253390837 | Clear | 10 | 19 | 94 |
| 3 | -1568431451 | Clear | 10 | 24 | 69 |

Full Unity Play Mode regression with both added cases: **137/137 PASS**, 0 failed, 0 skipped, **112.1077096 seconds**. Evidence: `full-playmode-results.xml`. The natural-combat case also passed in this full run, clearing three more ordinary 10-stage routes: seed 1347827555 (22 actions, 75 HP), seed 1688449067 (20 actions, 43 HP), and seed 947326876 (22 actions, 50 HP). The separate natural-defeat case passed in the same suite.

`source-hashes.csv` confirms that the final test source, `BattleManager.cs`, and `MainMenu.unity` in the workspace matched the isolated full-run copy byte-for-byte. The isolated `Battle.unity` was loaded during the run but was absent from the temporary project after Unity exited, so its post-run hash could not be compared; the workspace Battle scene was not edited for this milestone.

The runner creates a temporary project with product `SYNGRAVA_BattleTests` and a unique company name. This keeps test checkpoint, wallet, history and database separate from the user's profile. The first sandboxed attempt stalled before tests in Unity licensing and was stopped; the successful focused run used an elevated process against the isolated project. A subsequent full run reuses that imported copy with the final test source.

## Windows startup observation

The existing `Builds/Verification-2026-09-26/SYNGRAVA.exe` was launched through Windows Computer Use. Its first launch response timed out waiting for a targetable window; a subsequent window discovery found the game, and an actual 1920×1080 capture showed the portrait Main Menu, visible navigation, Continue at Stage 1/10, and Echo balance. Evidence: `windows-main-menu-1920x1080.jpg`.

**Main Menu rendering on the Windows build: PASS for this observation.** This upgrades the earlier batchmode-only startup evidence. A subsequent navigation attempt was interrupted by detected user input; the game window later disappeared. No successful menu click, scene transition, save mutation or combat playthrough is claimed from this UI session. Existing player progress was not deliberately replaced.

Before editing the tests, all six source/scene/build-setting hashes listed in the 26 September gate report matched the workspace. No new player build was needed for test-only changes.

## Reproduction and remaining gate

From the repository root, run `./Tools/Run-BattleSnapshotTests.ps1 -TestFilter TC_E2E_003` for the three-expedition case, `-TestFilter TC_E2E_004` for natural defeat, or omit the filter for the full regression. Seeds are generated by the normal new-run flow and recorded in the XML output as `NATURAL_COMBAT`; these are sampled coverage, not an exhaustive balance proof.

Continue with `Docs/StandardRun_ReleaseGate_Checklist.md`: three consecutive human-controlled Windows runs without debug combat, interactive Continue/Recovery and Clear/Defeat/Abandon reset checks, input/navigation and resolution review. **The manual release gate remains open and Infinite Casual is not unblocked by this report.** No Inspector assignment is required.
