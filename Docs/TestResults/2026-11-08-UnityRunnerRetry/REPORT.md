# Unity regression retry — 8 November 2026

## Scope

Re-run the isolated `Tools/Run-BattleSnapshotTests.ps1` Play Mode suite to verify the removal of Custom Skill CRUD from the player-facing Main Menu and preserve internal CRUD regression coverage.

## Environment and result

- The owner signed in to Unity Hub. Hub displayed the signed-in profile and listed `Singularity_AshenCycle` with Unity Editor `6000.6.0f1`.
- The runner copied `Assets`, `Packages`, and `ProjectSettings` into a temporary project with product identity `SYNGRAVA_BattleTests`, keeping the player's save path separate.
- Two attempts inside the Codex sandbox stopped before producing XML, including one after sign-in. Both showed Unity Licensing Client startup errors; the post-sign-in attempt also reported a missing Burst compiler pipe, and the first logged failures moving temporary files into the isolated project's `Library`. Those attempts are diagnostic only.
- The final retry used the same isolated runner outside the sandbox. Unity resolved entitlement details, compiled scripts successfully (`Tundra build success`), and completed the Play Mode suite. The log still contains entitlement lookup 404 messages and package/compiler warnings; the test process exited successfully and wrote a valid result XML.
- Final result: **151/151 passed, 0 failed, 0 skipped**, duration **119.0621764 seconds**. `TC_MENU_001_MainMenuActionsAndIdentityFitBothResolutions`, `TC_CRUD_001_SkillRecordsRoundTripThroughIsolatedDemoAndSQLite`, and `TC_E2E_003_ThreeExpeditionsUseRealCombatAndCommitTerminalResults` all passed.
- Evidence: `playmode-results.xml` and `source-hashes.csv`. SHA-256 matches confirm `MainMenuUI.cs`, `MainMenu.unity`, and `BattleSnapshotTests.cs` in the workspace matched the isolated test copy.

## Static checks

- Search in `Assets/Project/Scripts/UI/MainMenuUI.cs` and `Assets/Project/Scenes/MainMenu.unity` found no `ContentManagerUI`, `ContentManagerButton`, `OnContentManagerClicked`, or `CONTENT MANAGER` references.
- Scoped `git diff --check` for `MainMenuUI.cs` and `BattleSnapshotTests.cs` passed. Git printed only its existing LF-to-CRLF notices.
- Manual visual inspection in the Unity Editor and a fresh Windows build were not part of this Play Mode runner invocation. The follow-up build and standalone visual smoke check are recorded below.

## Follow-up verification — Windows build and visual smoke (8 November 2026)

- A fresh Unity `6000.6.0f1` Windows x86_64 Development build completed successfully from an isolated project copy. The first wrapper invocation built into its temporary project but failed to find the relative output path; rerunning with an absolute output path retained the final artifact correctly.
- BuildPipeline summary: **0 errors / 513 warnings**, **332,458,125 bytes**, **306.83 seconds**. Unity emitted 16 `CS0618` occurrences across four project call sites (`MainMenuUI.cs` and `BattleManager.cs`), package/tooling analyzer warnings, and five unsupported `Hidden/ChartRasterizerHardware` messages. The full log is `Builds/Verification-2026-11-08-MenuRemoval/unity-build.log`.
- Executable: `Builds/Verification-2026-11-08-MenuRemoval/SYNGRAVA.exe` (**667,136 bytes**), build identity `CodexGateTests/SYNGRAVA_MenuRemovalGate_20261108`. SHA-256: `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`. Gameplay assembly SHA-256: `1878C94E5221CAC5073728047FC97D1FDB5904619C76DBA6EF1701E5B7B34AFE`.
- Standalone startup smoke at **1920×1080** reached the Main Menu. Continue correctly displayed `NO SAVE`; New Expedition, Echo Archive, Settings, Quit Game, Run History, and Top Runs were visible. The Custom Skill/Content Manager entry was absent. This is a startup/menu visibility check, not a full interaction or gameplay acceptance run.
- At screenshot capture, the isolated player's `Player.log` had no exception, missing-script, or crash entry. It contains one Direct3D diagnostic: `d3d12: failed to query info queue interface (0x80004002)`. The captured screen shows the player at the Main Menu.

## Remaining acceptance

The formal Standard Run human acceptance gate remains separate and pending for this exact executable. The owner still needs to complete and record the required manual runs and transaction checks against this candidate; the 151/151 Play Mode suite and Main Menu smoke do not replace that gate.
