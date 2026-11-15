# Continue / Recovery and route readability

Implementation reviewed on 20-21 September 2026. Visual acceptance by the project owner remains pending; this report records implementation and automated verification, not final art approval.

## Functional changes

- Continue checks the current file again when clicked. A file changed or damaged after opening the modal cannot be restored from stale cached inspection.
- Continue, Recover and New Run ignore calls after the startup modal closes. A stale second click cannot rewind a battle.
- Recovery validates the backup, reconstructs temporary modifiers and effective resource limits, then writes the recovered primary before changing the live run. A failed write leaves the live run unchanged and retains the valid backup.
- The checkpoint remains a route-map save. This does not implement resuming halfway through an enemy turn or replaying an uncommitted reward.

## Presentation

- Continue/Recovery uses existing Fractured Fate torn-paper, eye and crystal elements around a quiet dark card. Text stays clear of decoration.
- Primary and secondary actions have different emphasis. Keyboard navigation remains between the two modal actions.
- Opaque startup and route backdrops hide unfinished battle labels and placeholder actors.
- The save summary shows stage, HP, mana and Embers. Raw seed values and implementation-oriented verification prose are removed.
- Route nodes align to stable lanes and stages. Each connection is a continuous line trimmed before its node plaque. Current choices are violet, completed paths are pale, future paths are dim.
- The graph and save identity are unchanged. Crossing lines still exist where actual branches connect; a crossing without an icon is not a selectable node.
- Node detail panels use player-facing names and descriptions. Internal route IDs such as BATTLE_01 are removed from presentation but remain in data.
- Rest, event and merchant descriptions honestly state their currently unimplemented interactions. This UI change does not add those gameplay systems.

## Verification

- First functional run: 78 passed, 0 failed.
- Full suite after UI and additional failure-handling tests: **81 passed, 0 failed, 0 skipped**, 45.87 seconds of test execution. See `final-results.xml`.
- Independent second pass of all seven Continue/Recovery cases: **7 passed, 0 failed**, 3.28 seconds. See `recheck-results.xml`. Hash comparison confirmed all five tested source/test files match the live project exactly.
- Cases cover real Battle scene reload and continuation, temporary HP modifier restoration, backup promotion, damaged primary and backup, repair/recheck, stale actions, and a locked temporary file during recovery.
- Both interfaces rendered in Unity at 1920x1080, 1280x720 and 1280x1024. Bounds checks passed. Recovery text overflow checks passed. The attached images were visually inspected at representative wide and narrow sizes.
- Tests ran in a disposable project with product name SYNGRAVA_BattleTests. The player's live save and database were not used as test fixtures.
- `git diff --check` passed. Existing user package, project-setting and asset changes were preserved.

## Asset service and remaining work

Higgsfield was attempted with the supplied map reference. Image generation returned `Requires basic plan or higher`; no new Higgsfield asset was generated. The implementation therefore reuses the previously approved local asset family.

No Inspector assignment or manual Hierarchy setup is required: BattleManager attaches the modal to the existing Canvas at runtime. Allow Unity to finish compiling and start a fresh Play session to see the updated runtime UI.

Not verified here: a standalone Windows build, a manual complete run on the player's machine, all possible display sizes, or final artistic acceptance. Next small task is visual review of these two screens, followed by the next documented gameplay milestone; new art can replace the separated ornament sprites when generation access is available.
