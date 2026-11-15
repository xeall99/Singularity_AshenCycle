# Settings, safe return to Main Menu, and automatic checkpoint

Session: 2–3 October 2026. Unity version: 6000.6.0f1. This work changes the current Settings UI and save/return flow without altering scenes, prefabs, packages, existing checkpoint schema, or the player's art placeholders.

## Behavior

- Settings now work from both Main Menu and Battle. Main Menu uses its existing Settings button and does not pause game time or show a second gear. Battle Settings pauses the run and restores the previous time scale on close or scene change.
- Gameplay rows explain the locked checkpoint and Analyze rules. Accessibility offers Reduce Motion; Graphics offers Windowed/Borderless and Unity quality presets; Audio offers Master Volume in 25% steps. Changes apply immediately and persist in a separate SQLite preferences database at `Application.persistentDataPath/Database/user_settings.db`. This avoids the content database that the Editor refreshes at startup.
- Battle Settings has a **SAVE & MAIN MENU** action. At an open map boundary, it writes and verifies an Active Run checkpoint before changing scenes. If the write fails, Settings remains open, the map shows its failed-save status, and the player can retry. During an entered node, combat, or an unresolved reward, leaving is blocked with an explanation so no incomplete transaction is saved as if the node were complete.
- At a finished run summary, leaving retries any pending Echo/History transaction and checks terminal checkpoint cleanup. Continue from Main Menu loads a saved active run at the map boundary. The Main Menu Settings panel has only a close action.

## Automated verification

- Full isolated Unity Play Mode regression after the map-layer fix: **149/149 PASS**, 0 failed, 0 skipped, 142.5501834 seconds. Evidence: `playmode-results-final.xml`, `unity-playmode-final.log`.
- Settings-specific coverage verifies menu versus battle pause behavior, category content, refusing mid-node exit, refusing exit after a simulated checkpoint write failure, successful autosave and Continue with the same RunId/HP/Mana, independent SQLite reads/writes, Reduce Motion on battle and Orbit animations, Master Volume application/persistence, and raycast access to the gear above the open map.
- `source-hashes-final.csv` records SHA-256 equality for all eight touched production/test files in the final test copy. Scoped `git diff --check` passed.
- An earlier reused isolated Unity Library had missing scene script bindings and failed test setup. A new isolated project imported from source and passed all 148 tests; the earlier XML/log are retained as diagnostic evidence, not counted as feature verification.

## Windows player verification

The first fresh build succeeded with 0 errors and 513 warnings, and its Main Menu Settings controls were visually inspected at 1920×1080. Audio changed 100%→75%, Reduce Motion changed OFF→ON, Display Mode changed Borderless→Windowed→Borderless, and Quality Preset changed Ultra→Very Low; all four values were present in the separate SQLite settings database. On entering the run map, the full-screen map overlay covered the gear. This was a real standalone usability defect despite the earlier 148/148 automated pass. The first build is retained at `Builds/Verification-2026-10-02-SettingsMenu/SYNGRAVA.exe`; `initial-player.log` has 0 missing-script messages and 0 unhandled exceptions. It is not the final candidate.

`StandardRunMapUI.Show` now raises the gear above the open map, and `TC_SET_007` checks it is the top graphic raycast target. The final build uses `CodexGateTests/SYNGRAVA_SettingsMenuGate_20261002` so its checkpoints and preferences cannot overwrite the player's normal profile.

Final executable: `Builds/Verification-2026-10-03-SettingsMenuFinal/SYNGRAVA.exe`, Windows x86_64 Development. Fresh isolated build **PASS**, 0 errors, 514 warnings, 332,435,398 bytes, BuildPipeline time 479.66 seconds. `build-source-matches-final.csv` confirms all eight changed source/test files matched the new build copy. The executable bootstrap SHA-256 is `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`; gameplay assembly `SYNGRAVA_Data/Managed/Assembly-CSharp.dll` SHA-256 is `8EB166285794FC5EF7FE5541F1BA818384AA4742290676A32E48AC0BD24C038C`. Unity's bootstrap hash is shared by earlier builds, so the assembly hash identifies this gameplay candidate. Build log: `Builds/Verification-2026-10-03-SettingsMenuFinal/unity-build.log`.

Interactive Windows smoke at 1920×1080:

| Check | Observed result |
| --- | --- |
| Settings reload | Main Menu showed the previously persisted 75% volume, Reduce Motion ON, Borderless display, and Very Low quality. The SQLite values matched; quality was then restored to Ultra through the UI. Evidence: `settings-before-final.json`, `settings-after-final.json`. |
| Continue existing test run | Menu offered Stage 1/10. Continue opened the same route map with its gear visible at bottom-left. The gear received a normal mouse click and opened the Battle Settings panel above the map. |
| Autosave before Main Menu | SAVE & MAIN MENU changed scene only after checkpoint write. RunId `146a03b8af764aaba8fe26a36be4d543`, seed `2016159697`, Stage 1, HP 100, Mana 5, active map boundary remained valid. The file SHA-256 changed from `955EFB8CB8D606601617156BB4D7D244C0A2721E5848968C6A82AE1567EDF2DE` to `938F32A509B34E0B1102B5FB022B8B5902D4F721FF2076580F0AE47B8BF684C8` because elapsed time was committed. Evidence: `checkpoint-before-final.json`, `checkpoint-after-save-menu.json`. |
| Continue after autosave | Continue returned to Stage 1 map. Checkpoint hash stayed exactly `938F32A509B34E0B1102B5FB022B8B5902D4F721FF2076580F0AE47B8BF684C8` after loading. Evidence: `continue-hash-check.json`. |
| Midbattle exit guard | Entered Ashen Wretch combat through normal input. SAVE & MAIN MENU kept Battle Settings open and displayed `FINISH THIS NODE AND REWARD BEFORE LEAVING.` The checkpoint hash remained unchanged. Evidence: `combat-rejection-hash-check.json`. |
| Player log | 0 missing-script messages, 0 exception/crash messages. `final-player.log`. Existing ComputeBuffer/D3D12 cleanup diagnostics appeared on normal shutdown. |

The owned player was closed after the midbattle rejection. Its valid Stage-1 test checkpoint remains available; quitting the unfinished test combat did not replace it. This smoke is a focused Settings/save test, not another full Standard Run or a human acceptance session.

## Scope and follow-up

This is a local Settings and checkpoint feature. It does not change the documented safe-save boundary to allow midbattle saves. No Inspector assignment or Hierarchy edit is required; the existing runtime UI builder creates the controls. At a map boundary, the close button still reads `RESUME BATTLE`; it closes Settings correctly but could be renamed `RETURN TO MAP` in a later wording pass. Human acceptance of the exact executable for the wider Standard Run release gate, final authored player/enemy art, tutorial, and other future milestones remain separate. No commit or push was performed.
