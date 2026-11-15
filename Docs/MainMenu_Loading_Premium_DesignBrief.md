# SYNGRAVA — Main Menu & Loading Screen: premium visual direction

Status: **design and Unity integration candidate for owner review**, 25 September 2026. The art direction still needs in-game visual acceptance. It extends `Docs/MainMenu_DesignPlan.md`; the existing Main Menu behavior remains authoritative.

## GDD anchors and composition decision

The GDD calls for minimalist dark high fantasy: black/charcoal base, deep purple Singularity energy, off-white legible text, strong silhouettes, controlled detail, thin fog, and broken orbit/circle motifs. The 1920×1080 layout is the reference canvas. Menu references 1–3 inform the division between functional navigation and cinematic art. Loading reference 4 informs the quiet centered composition. **Do not copy any reference's character, logo, icon, typography, or exact graphic treatment.**

Chosen concept: **The Fractured Threshold**. A lone Warden approaches an eye-shaped void inside a shattered stone orbit. The portal dominates the right visual field while the left is quiet enough for clean navigation. Loading removes the Warden and architecture; only the eye/orbit seal and a narrow progress system remain. No blood, gore, saturated rainbow lights, or busy decorative framing behind text.

| Token | Value | Use |
| --- | --- | --- |
| Void | `#08070D` | Full-screen background and deepest aperture |
| Slate | `#11101A` | Secondary surface and left legibility veil |
| Panel | `#191322` | Focused interactive region |
| Fracture | `#39294A` | Quiet border and rules |
| Abyss plum | `#5B3187` | Deep energy and distant orbit |
| Singularity violet | `#A96BEF` | Focus, selection, progress |
| Ash ivory | `#F3EFF7` | Logo, primary labels, eye highlight |
| Muted ash | `#B6ACC3` | Helper copy and secondary labels |

### Main Menu layout (1920×1080)

- Content-safe inset: at least 96 px horizontally and 68 px vertically. Keep title and all selectable labels inside the left 710 px visual-safe zone. The portal/character should not compete with the nav.
- Title block: `SYNGRAVA` at approximately x=104, y=145; `THE ASHEN CYCLE` as a small subordinate line. Use one original display wordmark, then a plain high-readability UI typeface for interactions. No type baked into the background plate.
- Primary stack: Continue Journey, New Expedition, Echo Archive, Run History. Four 548×82 px focus targets, x=102, beginning around y=382 with 16 px vertical gaps. Continue shows a save-specific helper when available; if absent, disabled styling and helpful explanation replace the active state. New Expedition must remain usable.
- Utility row below a thin divider: Top Runs, Settings, Quit Game. These remain visible but subordinate. Restore keyboard/gamepad focus clearly; focus is a violet edge + left marker, not a color-only change. Do not reintroduce Credits or Keybinds.
- Visual side: fractured arch and orbit ring centered roughly at x=1350, y=465. A small Warden silhouette near lower-right gives scale. Low luminance at screen edges; strongest ivory highlight belongs to the portal, second strongest to the title and focused option.
- Mobile/resolution adaptation is **not** part of this design milestone. At 1280×1024, keep a 4:3-safe adaptation: scale the art zone, narrow ornamentation, and retain full text and targets. Never crop essential menu actions.

### Loading layout (1920×1080)

- Keep more than half the field quiet charcoal-black. Center a 280–340 px original eye/orbit mark around x=960, y=380. Place `SYNGRAVA` wordmark under it. Use thin cracks only at the outer edges.
- A 580×6 px framed progress rail sits around y=753, with the actual filled width driven by Unity loading state. Three small orbit points may pulse, but never suggest real progress when loading is stalled.
- Status line under the rail: `OPENING THE NEXT PATH` (or localized equivalent). The lower hint zone may rotate short, useful game hints; no fake percentage. On error, replace the loading line with a readable explanation and a Retry action. Respect reduced-motion settings by replacing pulse with static contrast.
- Motion proposal for implementation: title and seal fade in over 250–400 ms; orbit turns 8–12 degrees during long loads; loading fill updates smoothly but never jumps backward; final transition is a 300 ms eye-aperture fade. Use `Time.unscaledDeltaTime` if this UI can appear while gameplay is paused.

## Detailed AI image prompts (Higgsfield or OpenArt)

The prompts are intentionally in English for direct use in either image model. **Generate plates and cutouts without text or UI** so Unity can render crisp, localized labels and accessible focus states. Color and lighting clauses are repeated to reduce drift. Use the finished Main Menu plate as a style reference for every later asset when the model supports image references.

### MM-01 — background plate, 16:9, ideally 3840×2160

> Original production background plate for SYNGRAVA, a minimalist dark-high-fantasy roguelite game main menu. A vast fractured black-stone threshold at dusk. A broken circular orbit around an ivory eye-like eclipse occupies the right half. Thin amethyst light leaks from deep cracks. One small lone cloaked Warden silhouette stands at lower right facing the portal. Controlled low fog, strong readable silhouettes, faceted low-poly-inspired 2.5D forms with painterly finish, crisp focal rim light, restrained texture. The left 40 percent is nearly black and visually quiet for a future title and vertical navigation. Exact palette direction: near-black charcoal #08070D, abyss plum #5B3187, muted violet #A96BEF, ash ivory #F3EFF7. Premium game key art, depth and atmosphere without visual noise. No words, letters, numbers, interface, menu, borders, progress bars, emblems from known games, copied characters, red/cyan lights, watermarks.

### LS-01 — loading background plate, 16:9, ideally 3840×2160

> Original near-black loading-screen background for the same SYNGRAVA world and visual style as MM-01. Extremely restrained charcoal void, barely visible fractured stone grain and fine diagonal cracks near outer edges, distant deep-plum mist, tiny pale violet rim light at the center fading into darkness. Center 60 percent must stay spacious and dark for a separate eye seal, wordmark and progress bar. Minimal dark high fantasy, soft controlled contrast, no architecture focal point, no character, no eye symbol baked in, no text, logo, buttons, progress bar, known franchise imagery, red, cyan or watermark. Palette #08070D, #11101A, #5B3187, #A96BEF and #F3EFF7 only.

### UI-01 — original seal/logo mark, transparent PNG, square 1024×1024

> A single original SYNGRAVA eye/eclipse-orbit emblem isolated on a truly transparent background. Off-white broken elliptical eye made of four angular stone-like slivers around a dark hollow center; one narrow violet core and two tiny orbit fragments. Symmetrical silhouette readable at 48 px, premium minimalist dark fantasy, controlled sharp geometry, no tiny engravings, no glow bleeding far beyond silhouette. Ash ivory #F3EFF7, muted violet #A96BEF and abyss plum #5B3187. Centered with 12 percent transparent padding on every side. No text, circle background, square tile, copied occult icon, other symbols, watermark.

### UI-02 — menu button borders/states, four separate transparent 1024×256 outputs

> One original wide angular button border for SYNGRAVA UI, isolated on transparent background, 1024×256, visual mass kept in the outer 12 percent and center completely transparent for live Unity text. Nearly black panel edge with one subtle broken-orbit corner and precise 2 px violet/ivory rim. Premium minimalist dark fantasy. 9-slice-friendly straight center spans and protected 96 px corner zones. Produce four consistent state variants as separate images: normal quiet charcoal/plum, focused bright violet left marker and ivory rim, pressed deeper plum with compressed light, disabled low-contrast slate. No lettering, icons, filled opaque center, bevel noise, excessive glow, or watermark.

### UI-03 — detail/modal frame, transparent PNG, 1536×1024

> One standalone wide framed panel border for SYNGRAVA, 3:2 transparent canvas. Four sparse angular torn-stone corners, a restrained plum hairline along edges, subtle ivory break at top center, interior transparent so Unity can supply a scalable dark panel fill and live text. Preserve straight mid-edge runs for 9-slicing, 120 px protected corners, generous empty area. Same lighting/palette as MM-01. No words, pictograms, background scene, paper texture covering the center, watermarks.

### UI-04 — small interface symbols, individually transparent 512×512

> Generate one symbol per image in the same SYNGRAVA icon family: Settings gear, Continue/route arrow, Archive eye-shard, History spiral/orbit, Top Runs laurel-shard, Retry broken-ring reconnect. Each symbol must be original, centered, monochrome ash ivory with a tiny controlled violet accent, clean silhouette readable at 32 px, no tile or background, transparent alpha, 15 percent padding. No text, gradients across thin lines, known game logos, watermark. Keep the six separate rather than a sprite sheet until sizes and alpha have been checked.

### UI-05 — loading rail and moving fill, separate transparent 1024×128 outputs

> Two separate horizontal UI elements for SYNGRAVA: first a thin empty progress rail frame in charcoal/plum with tiny angular ivory end caps, second a seamless violet-to-ivory luminous fill strip that can be cropped horizontally by the game. Transparent canvas, 1024×128, the visible rail is centered vertically and no thicker than 16 px. Keep a repeatable center, restrained premium glow, no baked percentage or status text, no background and no watermark.

## Assembly and implementation boundaries

Canva drafts: [Main Menu](https://canva.link/bkn7hfpoe414ndw) and [Loading Screen](https://canva.link/kvphk2fowlgl1gx). Both are one-page 1920×1080 visual concepts. Visual QA in the editor found a clear left menu stack and an uncluttered centered loading hierarchy. These are **composition references**, not a guarantee of separately exportable sprites or final licensed production art. Replace any baked copy and the logo with verified original/exportable elements before Unity implementation.

Figma wireframe: [SYNGRAVA — Premium Main Menu & Loading Screen](https://www.figma.com/design/xI1hFLmSDLowNdcseF5mkN). Page 1 contains the 1920×1080 Main Menu frame with a separate title, subtitle, primary navigation and utility row. Page 2 contains the 1920×1080 Loading frame with orbit, centered title, progress rail and status line. These are layout guides; the Canva designs and generated plates carry the visual mood. The Starter-plan MCP quota prevented a programmatic component system, so the frames were assembled in the Figma editor.

## Unity integration candidate

Two original, text-free plates are now in `Assets/Project/Resources/UI/MainMenuThreshold.png` and `Assets/Project/Resources/UI/LoadingVoid.png`. `MainMenu.unity` contains the Canvas, Input System EventSystem, background, title, panel, TextMeshPro labels, and Unity Buttons as editable scene objects. `MainMenuUI` binds those scene buttons at runtime: Continue and New Expedition open `MainMenuLoadingUI`; Echo Archive opens the existing unlock dialog; Run History opens `RunHistoryUI`; Top Runs opens `LocalLeaderboardUI`; Settings opens `BattleSettingsUI`; Quit uses the existing quit handler. Checkpoint-dependent summaries and dialogs are populated at runtime. The loading overlay reads `AsyncOperation.progress` during the real Battle scene load, then activates that scene. It exposes Retry and Back to Menu if loading cannot begin. Build Settings now register MainMenu and Battle, replacing a stale SampleScene entry.

The generated plates are production candidates, not approved final art. Unity Main Menu renders at 1920×1080 and 1280×1024 have been inspected for artwork crop, button containment, and text overflow; owner art acceptance and a manual Windows build check are still pending. Loading composition, focus navigation, and overlay transitions should also be checked in an interactive playthrough. The Figma wireframe intentionally does not embed the full-size raster plates.

### Implemented button artwork — 25 September 2026

The seven scene buttons now use one original, transparent fractured-obsidian plate at `Assets/Project/Resources/UI/MenuButtonFracturedFrame.png`. Unity crops its alpha bounds into a reusable sprite and stretches it across the two primary, four utility, and one compact button. Text remains live TextMeshPro, while Unity Button color states tint the same plate for hover, press, focus, and disabled states. This keeps the visual language consistent without baking UI copy into art. Main labels and helper copy were moved inward after render review; the compact **TOP RUNS** button omits helper copy so it remains legible. The runtime-created menu fallback uses the same sprite and spacing.

This is a single-plate implementation of the UI-02 direction, not four independently illustrated state sprites or true 9-slice art. Separate state variants, dedicated small icons, and motion polish remain optional future art tasks. Focused menu tests passed 5/5; the complete Play Mode suite passed 133/133. Both 1920×1080 and 1280×1024 renders were reviewed, with evidence in `Docs/TestResults/2026-09-25-MainMenuButtons/`.
