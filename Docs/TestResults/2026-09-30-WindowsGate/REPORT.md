# Windows Standard Run gate follow-up — 30 September 2026

Status: **partial PASS; release gate remains open**. Automated regression, a Windows build, and focused interactive save-flow checks succeeded. The three consecutive human-controlled full Standard Runs, final Clear/Defeat inspection, 1280×1024 visual check, and an ordinary human Alt+F4 exit check are still pending. Do not begin Infinite Casual on this evidence alone.

## Build and isolation

- Final tested player: `Builds/Verification-2026-09-30-RecoveryFix/SYNGRAVA.exe`, Windows x86_64 Development Build, Unity 6000.6.0f1. `unity-build.log` reports **succeeded, 0 errors, 514 warnings**, size 332,433,831 bytes, build time 349.94 seconds.
- The player was compiled from an isolated project with `companyName: CodexGateTests` and `productName: SYNGRAVA_GateTests_20260930` set **before** the build. The resulting `SYNGRAVA_Data/app.info` has those values. `source-hashes.csv` records matching SHA-256 values for the workspace and isolated build/test source, including MainMenu/Battle scenes and BattleManager.
- The first compiled profile-isolated player (before the Recovery fix, but with matching company/product identity) showed **NO SAVE / ECHO 0** on a fresh launch. After New Expedition, it wrote `Saves/syngrava-active-run-v0.4.json` under `%USERPROFILE%\AppData\LocalLow\CodexGateTests\SYNGRAVA_GateTests_20260930`. The fixed player reused and updated that same profile during Recovery and Abandon checks. This empirically verifies test-save separation; the original player profile was not edited.
- An earlier copy of the 26 September executable only changed `app.info` after compilation. Its save isolation was not established. Do not use that copy for gate playtests.

## Automated verification

- Natural-combat baseline from 27 September: **137/137 PASS**, including six sampled full expeditions using ordinary action/reward listeners and a natural Defeat. See `Docs/TestResults/2026-09-27-NaturalCombatGate/REPORT.md`.
- New bug-specific test `TC_RECOVERY_004A_MainMenuContinueWithDamagedPrimaryOffersRecovery`: **1/1 PASS** in `focused-recovery-results.xml`.
- Full Play Mode suite after the fix: **138/138 PASS**, 0 failed/skipped, duration 138.11 seconds, in `full-playmode-results.xml`. Tests ran in an isolated Unity copy; these are not human playtests.

## Interactive Windows checks at 1920×1080

| Check | Result | Evidence / limit |
| --- | --- | --- |
| Fresh profile and checkpoint (pre-fix player) | PASS | Menu showed NO SAVE / ECHO 0, then New Expedition wrote a test-profile checkpoint. |
| Continue (pre-fix player) | PASS | Relaunch showed STAGE 1/10; Continue returned to map node 1/10. |
| One ordinary battle/reward (pre-fix player) | PASS | Two Shadow Strike button clicks defeated Ashen Wretch (40 HP); a Vital Core reward was selected. The committed checkpoint reached depth 2 with HP 103, mana 2, 10 Embers, one modifier, one battle win and two actions. |
| Recovery from damaged primary | PASS after fix | With only the **test-profile** primary truncated and its valid `.bak` retained, Main Menu Continue showed RECOVERY AVAILABLE, stage 1/10, HP 100, mana 5, Embers 0. The primary remained damaged until RECOVER RUN was clicked. Then a valid stage-1 primary was written and the map opened. |
| Abandon / New Expedition | PASS | With a valid active checkpoint, confirmation appeared. Cancel kept Continue available. Confirm created a different RunId (`e01c5e2023204d25889a83ae02e69cad` → `ae5ad1036aa44717a240a146e1fe1a1f`) and reset depth 1, HP 100, mana 5, Embers 0, wins 0, modifiers 0. |
| Main Menu Quit Game | PASS | The rebuilt player exited normally without a crash entry in Player.log. |

An initial Battle screenshot caught Shadow Strike during its entrance animation, when it was partly below the viewport. After the animation ended, Basic Attack, Shadow Strike, Guard, and Analyze were fully visible and usable; this was a transient animation frame, not a confirmed layout defect.

## Bug fixed and remaining risk

The first compiled player exposed a recovery bug: `BattleManager.PresentStartupRunChoice` treated `RecoverableBackup` as an ordinary Continue intent. It loaded the older backup directly into the map without showing the recovery choice or repairing the truncated primary. The condition now accepts only `CanContinue` for direct load; `RecoverableBackup` reaches the existing recovery overlay and its explicit promotion path. The new test and rebuilt-player interaction verify both states.

During prior Alt+F4 attempts sent through Windows UI automation, the first player lingered during shutdown and a later attempt logged a native crash after Input System shutdown. `alt-f4-crash-Player.log` preserves that evidence. Its stack includes UnityPlayer/Windows UI Automation frames, but does not identify a proven project-code cause. The rebuilt player's in-menu Quit Game exited cleanly. A human Alt+F4 attempt on the current build is still needed to classify this exit-path risk.

`checkpoint-after-node1.json` and `checkpoint-backup-before-node1.json` are copies of generated **test-profile** checkpoints made before deliberate corruption. The active test-profile checkpoint now contains a fresh stage-1 run after the Abandon check.

## Remaining gate

Use `Docs/StandardRun_ReleaseGate_Checklist.md`: three consecutive human-controlled 10-node runs through Boss and Summary, with History/Top Runs, Clear/Defeat, save transactions, visual/input checks (including 1280×1024 if available), and an Alt+F4 exit check. Record seed, RunId, route, result, score, Echo, and logs. Automated or tool-driven runs above do not count toward the three human runs. Infinite Casual remains on hold.
