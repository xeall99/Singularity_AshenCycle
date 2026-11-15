# Progressive route reveal and Rest recovery

## Implemented behavior

Only the completed route and immediate reachable choices are displayed. Future nodes and future connections remain hidden, including while scrolling. On a new run only the starting node is shown. Resolving a node and its reward reveals the next set of choices. Unchosen earlier branches disappear.

Node types remain determined by the existing run seed. Revealing, reopening the map and continuing a saved run do not reroll the route. There is no save-schema or graph topology migration.

Lane centers remain aligned; stage spacing increases to 190 UI units. Only travelled and immediately available connections are visible. Decorative spine prose and tiny node state captions are hidden so text cannot sit across connection lines. The detail panel still identifies the selected stage and encounter.

Rest provides two alternatives: restore half of effective maximum HP, or half of effective maximum Mana. Fractions round upward and resources are capped. Example: 20/100 HP becomes 70/100; 95/100 becomes 100/100. Mana 1/10 becomes 6/10. Choosing one completes the Rest, saves the resulting boundary, and reveals the next stage. Repeating a callback for that Rest cannot grant more recovery.

## Verification

Two complete isolated Unity PlayMode runs passed 85/85 tests each, with zero failures. The second includes a render of the Rest panel at 1280x1024 and checks its text and controls remain inside the detail panel with no text overflow. The map also renders at 1920x1080, 1280x720 and 1280x1024.

Four Rest cases exercise HP and Mana recovery, capping near maximum, stale callbacks, persisted resource values, and the count of visible visited/reachable nodes. Existing save, recovery, branch generation and battle regressions remain in the full suite.

The last cosmetic change removes node captions intersected by vertical connectors. A focused UI/Rest rerun passed **9/9**, zero failures; see `rest-visual-results.xml`. Hash comparison confirmed all three changed scripts/tests match the working project. Tests use the isolated SYNGRAVA_BattleTests product identity, not the player's save.

## Setup and limits

No manual Inspector assignment or scene edit. Restart Play Mode after compilation. Files changed: StandardRunMapUI.cs, BattleManager.cs, BattleSnapshotTests.cs and PROJECT_CONTEXT.md.

No standalone Windows build or manual end-to-end run was performed. Rest recovery is at map boundaries, not a mid-battle heal. Random Event and Merchant gameplay remain outside this change. Final visual acceptance remains with the user.
