# Run Summary navigation build — 13 November 2026

## Change

`Assets/Project/Scripts/UI/RunSummaryUI.cs` now builds three terminal actions: **BEGIN NEW JOURNEY**, **MAIN MENU**, and **EXIT GAME**. Main Menu returns to the existing `MainMenu` scene. It stays disabled while Echo or History persistence is pending, so the existing retry action remains available. Exit uses the Unity Editor stop action in Editor and `Application.Quit()` in a player build.

## Windows build

- Unity Editor: `6000.6.0f1`
- Build: Windows x86_64 Development
- Result: **0 errors / 513 warnings**, 332,447,298 bytes, 563.32 seconds
- Executable: `Builds/Verification-2026-11-13-RunSummaryNavigation/SYNGRAVA.exe`
- Save identity: `CodexGateTests/SYNGRAVA_MenuRemovalGate_20261108` (preserves the existing local History and Echo profile)
- `Assembly-CSharp.dll` SHA-256: `D56A7CC663442CC3945C1489E30E383B730A99762455CD11FEE133352953EF0A`
- Full build log: `Builds/Verification-2026-11-13-RunSummaryNavigation/unity-build.log`

## Verification limits

The previous 8 November executable was built before this change and does not include the new actions. The new build compiled successfully and its gameplay assembly hash differs from that candidate. The new summary screen has not yet been opened in this build, and no Play Mode tests or manual UI tests were run for this change. A clear run in the new build is still needed to inspect button layout and Main Menu/Exit interaction.
