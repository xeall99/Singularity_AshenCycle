# Main Menu button art — verification report

Date: 25 September 2026  
Unity: 6000.6.0f1  
Scope: image and typography of the seven Main Menu navigation buttons

## Result

The Main Menu scene displays a reusable fractured-obsidian button sprite on Continue Journey, New Expedition, Echo Archive, Run History, Settings, Quit Game, and Top Runs. The existing Unity `Button` components, TextMeshPro labels, and click handlers remain connected. `MainMenuUI.cs` uses the same sprite and text offsets if the visual hierarchy is built at runtime.

The first render exposed helper copy against the lower border. Both scene and fallback layout were corrected. The compact Top Runs button now carries only its primary label. Final Unity captures at 1920×1080 and 1280×1024 were reviewed for legibility, alignment, panel containment, and artwork crop.

## Asset and implementation

- Asset: `Assets/Project/Resources/UI/MenuButtonFracturedFrame.png` (transparent PNG, 1983×793 source image); Unity imports the centered plate as a sprite with rect `(82, 208, 1814, 401)`.
- Generation mode: `stylized-concept`, original text-free UI asset. Prompt asked for one horizontal fractured black-stone plate with a charcoal-plum inset, sparse amethyst/ivory edge accents, visually quiet center, transparent exterior, and no lettering or mockup.
- The sprite uses `Image.Type.Simple`; it is stretched to each existing button rectangle. A single sprite is tinted for normal, highlighted/selected, pressed, and disabled states. This does **not** claim four separately illustrated button states or nine-slice scaling.
- The serialized scene and the fallback builder were both updated. All seven buttons continue to use their existing functional click targets.

## Automated verification

| Run | Result | Purpose |
| --- | --- | --- |
| Focused `TC_MENU_` Play Mode | **5/5 passed**, 0 failed, 0 skipped | Main Menu scene, sprite presence, layout at two resolutions, Continue/New Run/Settings paths |
| Full Play Mode suite | **133/133 passed**, 0 failed, 0 skipped | Regression across game systems after the final scene and code edits |

Unity ran in batchmode on an isolated copy of the project so the user's open Editor and workspace `Library` were not disturbed. `source-hashes.csv` compares the tested code, sprite, sprite metadata, test source, and scene with the workspace. Four entries match byte-for-byte. The scene is textually identical after CRLF/trailing-whitespace normalization; `scene-normalized-match.json` records this check. The workspace copy removed Unity's trailing spaces after save, which explains the raw scene hash difference.

## Evidence

- `focused-menu-results.xml`
- `full-playmode-results.xml`
- `main-menu-1920x1080.png`
- `main-menu-1280x1024.png`
- `source-hashes.csv`
- `scene-normalized-match.json`

This verifies the Play Mode UI and code paths tested here. No Windows standalone build or full manual game playthrough was performed for this visual iteration; final art approval remains with the owner.
