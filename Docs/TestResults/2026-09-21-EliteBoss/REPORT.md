# Elite / Boss and encounter cut-ins

Date: 21 September 2026. Unity 6000.6.0f1. Tests used a separate product/company identity in an isolated project; player saves were not used.

## Result

Initial implementation: 95/95 PlayMode tests passed in 55.64 seconds. Final implementation after banner refinement and additional animation coverage: **96/96 passed in 60.17 seconds**, zero failures/skips. Evidence is preserved in first-results.xml and final-results.xml. The user authorized testing as necessary for this milestone, superseding the prior two-run restriction.

The final run covers deterministic Elite/Boss damage patterns, Boss charge without damage, awakening at half HP, preservation of the already prepared action, input lock during introduction, opposite slide directions, static reduced-motion presentation, pause holding the cut-in, and cancellation/replacement without stale animation cleanup. Existing battle, Orbit, reward, map, Merchant, event and checkpoint regressions also passed.

Actual Unity images boss-banner-1920.png and boss-banner-1280.png were visually inspected. Banner labels fit at 1920x1080 and 1280x1024; automatic text overflow and label bounds assertions passed. Images show a held animation frame, not an exported video. Direction and lifecycle are verified by the animation test.

## Gameplay and presentation

Elite alternates normal damage with a 135% heavy strike. Boss cycles base strike, zero-damage charge, then 150% rupture. After a surviving enemy turn at half HP or below, a single phase-II announcement enables 125% strikes and 175% ruptures for future prepared actions. Damage rounds up. Guard remains effective. HP values remain database-owned; no HP inflation was added.

Elite enters from the left and exits right; Boss enters from the right and exits left. A tilted black/violet band, bright accent rails, authored node emblem and enemy name use the established SYNGRAVA palette. Boss accents are crimson. The intro lasts 2.1 scaled seconds. Pause freezes it; Motion Effects OFF removes translation. No external animation package, new character art or audio asset was added.

## Files / setup

- EnemyActionPlan.cs: immutable named encounter actions and deterministic cycles.
- BattleManager.cs: sequence/phase lifecycle, cut-in dispatch, input guard and cancellation.
- EncounterBannerUI.cs (+ meta): reusable native Unity UI animation.
- BattleSettingsUI.cs: exposes the existing Motion Effects toggle to the cut-in.
- BattleSnapshotTests.cs: new encounter and rendering coverage; combat-only navigation fixtures bypass introductions where presentation is outside their scope.
- PROJECT_CONTEXT.md: design decisions, implementation scope and verification status.

All five production/test file hashes match the successful isolated run. The banner attaches to the existing battle Canvas at runtime, and its emblem uses StandardRunMapVisualAssets. No manual Inspector assignment or package installation is required. Existing unrelated worktree changes were preserved; no commit/push was performed.

Figma layout/motion specification: https://www.figma.com/design/cFuAmGd1jgxacnD53crytC (Elite 3:2, Boss 3:8). The Unity render is the final implementation evidence; the Figma frames document composition and movement rather than exact final pixels.

## Limits / next step

Owner approval of visual style and balance, standalone Windows build and manual complete-run evaluation remain outstanding. Combat character sprites are still existing placeholders. A bespoke Boss portrait, arena illustration, entry impact/whoosh sound and phase-change music would be the next production assets for richer cinematic presentation; they are not prerequisites for this implementation. Relic/Anomaly/Status integration remains subsequent gameplay work in the context plan.
