# SYNGRAVA Main Menu — minimalist portrait composition

**Status:** implemented in `Assets/Project/Scenes/MainMenu.unity`; awaiting the owner's visual review.  
**Reference canvas:** 1920 × 1080, top-left origin.  
**Latest direction:** reference image 1 sets the left navigation and right character composition. Reference image 2 informs the Warden's face, black hair, pale eyes, and silver halo. Neither reference is used as an in-game asset.

The full-screen portrait is nearly black on the left so the controls sit directly over it. The Warden's face occupies the right side. There is no separate eye ornament, large menu card, torn-paper border, portal image, or permanent UI over the portrait. The image contains no baked buttons or words; every action remains live Unity UI.

## Scene layer tree

```text
MainMenuCanvas [CanvasScaler: Scale With Screen Size, 1920×1080, Expand]
├─ Background [solid void fallback]
├─ WardenPortraitBackdrop [generated portrait, AspectRatioFitter: EnvelopeParent]
└─ MenuCard [transparent full-canvas layout host]
   ├─ TitleText / SYNGRAVA
   ├─ SubtitleText / THE ASHEN CYCLE
   ├─ ContinueButton / CONTINUE EXPEDITION + concise save state
   ├─ NewRunButton / NEW EXPEDITION
   ├─ EchoArchiveButton / ECHO ARCHIVE + concise Echo balance
   ├─ SettingsButton / SETTINGS
   ├─ QuitButton / QUIT GAME
   ├─ RunHistoryButton / RUN HISTORY
   └─ TopRunsButton / TOP RUNS
```

Each action contains a live `Label`, `Summary`, 3 px `FocusRail`, and 1 px `Rule`. `MainMenuActionVisual` supplies restrained mouse/gamepad focus movement and color, while `MainMenuUI` binds the existing Continue, New Expedition, Archive, Settings, Quit, History, and Top Runs handlers. Dialogs appear only after an action is chosen.

## Reference coordinates and spacing

| Element | Bounds `(x, y, width, height)` | Placement rule |
| --- | --- | --- |
| Title | `(96, 106, 480, 88)` | Left aligned, 68 px; inside the 5% screen inset. |
| Subtitle | `(100, 208, 450, 30)` | 17 px; 14 px below title bounds. |
| Continue | `(96, 350, 456, 60)` | 28 px text inset; compact `NO SAVE` or `STAGE n/n` at right. |
| New Expedition | `(96, 428, 456, 60)` | 18 px vertical breathing space after previous row. |
| Echo Archive | `(96, 506, 456, 60)` | Same rhythm; balance is right aligned. |
| Settings | `(96, 584, 456, 60)` | Same row rhythm. |
| Quit Game | `(96, 662, 456, 60)` | Same row rhythm. |
| Run History | `(96, 786, 214, 46)` | Secondary action, 64 px below main stack. |
| Top Runs | `(336, 786, 214, 46)` | 26 px horizontal gap from Run History. |

Menu rows have no image sprite or thick border. Their unfocused surface is low-contrast translucent black; a focused row gains a thin violet rail, a quiet aura, a +8 px label shift over 150 ms, and a brighter bottom rule. Disabled Continue remains visible but subdued. Initial focus goes to Continue only when a verified save exists; otherwise it goes to New Expedition.

The controls have been checked at 1920×1080, 1280×1024, and 1280×720. The portrait may crop on non-16:9 screens, but the complete action stack remains inside the canvas and text does not overlap at those resolutions. The menu is designed for PC first; console typography and overscan should be reviewed on the target device before claiming console readiness.

## Design tokens

| Token | Value | Usage |
| --- | --- | --- |
| Void fallback | `#0B0914` approximate | Covers art loading or failure. |
| Primary text | `#F5F0FF` approximate | Wordmark. |
| Muted action | `#ADA6BD` approximate | Unfocused menu text. |
| Subtitle and focus | Soft violet `#D99AFF` / `#A370E3` | Small accent only. |
| Pressed | Muted ember gold `#D4AF38` approximate | Momentary action feedback. |
| Focus transition | 150 ms, unscaled time | Works while game time is paused. |
| Row rule | 1 px at low opacity | Separates rows without a box frame. |

The earlier 30/70 blueprint, top-right badges, large version footer, and threshold artwork were superseded by the owner's later request for a cleaner portrait menu. The current Unity scene and the render evidence in `Docs/TestResults/2026-09-26-MainMenuRemake/` are the implementation reference.
