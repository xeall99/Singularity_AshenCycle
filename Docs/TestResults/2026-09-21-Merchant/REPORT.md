# Merchant and unified route layout

Date: 21 September 2026. Unity 6000.6.0f1, isolated PlayMode project with a separate product/company identity. Player saves were not used.

## Implemented behavior

- Map/sidebar gutter removed: route right edge and detail left edge both equal 387 UI units relative to the card. Existing viewport/node positions remain unchanged.
- Continuous upper/lower torn-paper ornaments reuse approved SYNGRAVA sprites. No replacement texture or Inspector assignment is needed.
- Merchant: buy one HP supply (12 Embers, 50% effective maximum HP), buy one Mana supply (8 Embers, 50% effective maximum Mana), or leave freely. Prices are provisional. Recovery rounds upward and caps at maximum. Full resources and insufficient balance reject purchases.
- Resolved purchases/leave commit resources and visited route history to the existing checkpoint. Continue restores that result. Repeated or foreign transactions are rejected.

## Exactly two Unity runs

| Run | Passed | Failed | Skipped | Duration |
| --- | --- | --- | --- | --- |
| First | 93 | 0 | 0 | 54.58 s |
| Second | 92 | 1 | 0 | 51.75 s |

Both runs used unchanged production and test code. All four dedicated Merchant test cases passed in both runs, including HP purchase, Mana purchase, leave, affordability, resource caps, invalid/repeated choices and save/Continue. The combined event/Merchant layout capture test passed in both runs; it checks shared-edge alignment, text overflow and panel containment at 1920x1080 and 1280x1024.

The second run exposed a legacy branch-navigation fixture assumption in `TC_BRANCH_003_MapOffersConnectedChoicesAndCommitsSelectedNode`: its randomly seeded choice was `Merchant_02_L2`, but it invoked the primary purchase button on a fresh run without sufficient funds. The transaction correctly did not resolve the merchant; the test expected it to advance as though the old placeholder were still active.

After diagnosis, that fixture was changed to use `LeaveMerchantButton` for Merchant nodes and assert that the selected navigation action is interactable. No production code changed after the two runs. **The amended fixture has not been rerun** because the user explicitly limited this task to two test executions. A complete green regression after that test-only correction remains outstanding; this report does not claim two successful full runs.

Production file hashes matched the isolated copies. The test file now differs solely by the documented fixture correction. XML evidence is preserved without modification as `first-results.xml` and `second-results.xml`.

## Visual/design evidence

- `merchant-1920x1080.png` and `merchant-1280x1024.png`: visually inspected Unity captures; all three controls fit and the empty gutter is gone.
- `event-seam-1920x1080.png`: visually inspected Ashen Cache capture corresponding to the user's panel complaint.
- Figma layout: https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=2-2. Preview inspected. Uses Roboto as a layout substitute; Unity keeps Liberation Sans and existing art.
- Canva searches found no existing SYNGRAVA design or brand template. No Canva asset was generated or imported.
- An additional Figma programmatic check was not executed because automatic approval review hit a usage limit. File creation, layout creation and preview capture had already succeeded.

## Files and remaining scope

RunMerchantService.cs (+ meta), RunManager.cs, BattleManager.cs, StandardRunMapUI.cs, BattleSnapshotTests.cs and PROJECT_CONTEXT.md. Existing unrelated project edits were preserved. No commit or push performed.

This milestone is limited to one resource purchase per merchant visit. Relic stock, shop rerolls and permanent economy remain future scope. Dedicated Elite/Boss encounters are next in the integration plan. Standalone Windows build, manual full-run validation and owner visual/balance acceptance are not claimed.

## Authorized additional verification

The user explicitly approved one additional run after the two-run limit was reached. The third full isolated Unity PlayMode run passed **93/93**, with zero failures or skips, in **52.20 seconds**. `TC_BRANCH_003_MapOffersConnectedChoicesAndCommitsSelectedNode` passed with the corrected navigation fixture. Evidence: `third-results.xml`.

Hashes of RunMerchantService.cs, RunManager.cs, BattleManager.cs, StandardRunMapUI.cs and BattleSnapshotTests.cs match the successful isolated run. No code changes were needed during this additional verification. This supersedes the earlier pending-regression status; the first and second results remain preserved as historical evidence. Standalone build and manual full-run checks remain outside this verification.
