# Main Menu portrait remake — 26 September 2026

## Result

The existing `MainMenu.unity` scene now uses a quiet, left-aligned navigation over an original Warden portrait. The layout follows the supplied first image's left menu/right face composition. The supplied second image guided the character's face, black hair, pale eyes, and silver halo. The old threshold background, standalone eye, large opaque card, torn ornament strips, decorative button sprites, and redundant footer/navigation copy are absent from the live menu.

Main actions have 18 px vertical gaps and their own text padding. Run History and Top Runs sit together below the main stack with a 26 px gap. The focused button has a restrained violet rail and 150 ms text shift. Continue is disabled when no verified checkpoint exists; New Expedition receives initial focus in that state. The existing Continue, New Expedition, Echo Archive, Settings, Quit, Run History, and Top Runs handlers remain bound.

## Files

- `Assets/Project/Scenes/MainMenu.unity`: serialized menu hierarchy and placement.
- `Assets/Project/Resources/UI/MainMenuWardenPortrait.png` and `.meta`: original portrait art, imported as a Unity sprite.
- `Assets/Project/Scripts/UI/MainMenuActionVisual.cs` and `.meta`: focus/hover/pressed feedback.
- `Assets/Project/Scripts/UI/MainMenuUI.cs`: runtime fallback visual parity, concise save/Echo summaries, initial selection, and old ornament helper removal.
- `Assets/Tests/PlayMode/BattleSnapshotTests.cs`: scene identity, interaction availability, text fit, and three resolution captures.
- `Docs/MainMenu_AAA_Composition_Blueprint.md`: current coordinates and visual rules; earlier top-right badge plan superseded.

## Verification

Unity 6000.6.0f1 ran against a separate temporary project with product name `SYNGRAVA_BattleTests`, protecting the user's live save data and Editor session. The scene, code, sprite, metadata, and test source matched that isolated project byte-for-byte by SHA-256 (`source-hashes.csv`).

| Check | Result |
| --- | --- |
| Main Menu focused Play Mode tests | 5 passed, 0 failed, 0 skipped (`focused-menu-results.xml`) |
| Full `BattleSnapshotTests` Play Mode suite | 133 passed, 0 failed, 0 skipped (`full-playmode-results.xml`) |
| Render at 1920×1080 | Inspected; clear left navigation and right-side portrait (`main-menu-1920x1080.png`) |
| Render at 1280×1024 | Inspected; actions remain inside canvas (`main-menu-1280x1024.png`) |
| Render at 1280×720 | Inspected; no action/text overlap (`main-menu-1280x720.png`) |
| C# compilation | Passed in Unity; no new compile errors |

The screenshots are automated camera captures during Play Mode tests, not a manual standalone playthrough. A Windows player build and device-specific console overscan review were not run for this visual iteration. No scene Inspector assignments remain: the new sprite and action component are already serialized in `MainMenu.unity`.

The generated portrait was guided by the two user-supplied visual references but does not use either screenshot directly as an in-game texture. The design is ready for the owner's visual review before recording it as an accepted GDD direction.
