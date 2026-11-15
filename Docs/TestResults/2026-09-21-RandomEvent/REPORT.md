# Random Event milestone

## Scope and code ownership

The newer GDD integration sequence places Random Event after Continue/Recovery and Rest. This milestone completes that resource-choice flow; Merchant, Relic, Anomaly and Status Effect remain separate work.

- RunEventService.cs: immutable content catalog, stable seed/node selection, affordability, resource calculation and one-time node resolution. It has no Unity UI or filesystem dependency.
- StandardRunMapUI.cs: two choices and exact outcomes in the existing detail panel; unavailable actions disabled. Rest and Event share the alternative-button layout without duplicating the panel.
- BattleManager.cs: dispatch, resource/HUD synchronization, checkpoint after resolution, then map reveal.
- BattleSnapshotTests.cs: deterministic/service cases, actual UI selection with save/Continue, six layout captures, and battle regression coverage.

## Content

| Encounter | Choice A | Choice B |
| --- | --- | --- |
| The Ember Altar | Spend 10 HP for 15 Embers, retaining at least 1 HP | Gain 3 Embers |
| The Silent Well | Spend 3 Mana to heal 25% maximum HP | Restore 2 Mana |
| The Ashen Cache | Gain 10 Embers | Heal 15% maximum HP |

Values are a provisional balance baseline. Healing rounds upward and is capped at effective maximum HP. Mana cannot exceed its maximum. The choice commits once, records resulting resources and visited node in the existing checkpoint, and reveals the next reachable nodes. Uncommitted event content is stable for a given seed and node code. No separate choice journal or schema migration is introduced.

## Verification

Final unchanged code passed two complete isolated Unity PlayMode runs, per the user's request: **89/89** in the first (50.89 seconds) and **89/89** in the second (50.10 seconds), both with zero failures or skips. See first-results.xml and second-results.xml. No third final run was performed. Source hashes matched the working project; git diff --check passed.

Coverage includes 60 seeds/all three encounters, rejected invalid indices and unaffordable costs without mutation, repeated resolution rejection, both UI choices, disk checkpoint equality, Continue without duplicate rewards, and all existing battle/save/map/Rest regressions.

Every encounter was rendered at 1920x1080 and 1280x1024. Text-overflow and panel-bound checks passed in the first final run; representative wide and narrow captures were visually inspected.

During development, three legacy combat tests failed because their navigation helper assumed noncombat nodes had no effects. The helper now explicitly bypasses optional noncombat effects for battle-only assertions. Separate Event/Rest tests exercise the real UI, costs and persistence. This scope change is not a claim that events have no effects.

The test project uses SYNGRAVA_BattleTests identity, protecting the live player's saves and database. No additional package, scene edit, or Inspector assignment is required.

## Remaining limits

Standalone Windows build and a manual complete run were not performed. Event illustrations, status/relic event outcomes, a choice-history journal and final balance tuning remain future work. The next item in the newer integration sequence is Merchant and resource economy.
