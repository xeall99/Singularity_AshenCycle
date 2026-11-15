# Relic System — 22 September 2026

## Scope and decisions

The user requested four different relics, an optional Relic node alongside other route choices, and selection through the existing three-card upgrade interface. This milestone implements that vertical slice. It does not add relic rewards to Elite encounters or replace existing battle upgrades.

New runs contain one Relic Shrine alternative at stage 3. It is a fourth lane, copying the incoming/outgoing connections of the original left-lane encounter. The original encounter remains selectable. The shrine is accessible only through connected predecessors; it is not a free teleport from every branch. Progressive reveal still hides it until that stage is available. Choosing it consumes one stage, preserving the 8–10-stage run length and mandatory late Elite/Rest/Boss encounters.

The canonical route retains its original encounter quotas. A route through the shrine substitutes one early encounter with a relic; its actual encounter totals therefore differ by that one encounter. Graph validation checks compatibility against the substituted encounter, rather than falsely treating the relic as an extra battle.

## Four relics and balance boundaries

| Relic | Effect | Constraint / intended use |
| --- | --- | --- |
| Hollow Crown | Guard restores 1 extra Mana; total immediate Guard recovery is 3 | Cannot exceed effective maximum Mana. Benefits Guard-oriented builds; does not generate Embers or permanently raise maximum Mana. |
| Ashen Fang | A successful Basic Attack adds 1 Burn stack for 2 enemy turns; each stack deals 1 damage after the enemy acts | Maximum 3 stacks, reapplication refreshes duration. Player defeat resolves before Burn. No carryover to the next battle. |
| Ember Locket | Restore 6 HP at battle entry | Flat amount, capped at effective maximum HP. No instant heal on acquisition and no increase to maximum HP. |
| Fractured Lens | First successful Skill in each battle deals 6 extra damage | One proc per battle. Insufficient-Mana attempts do not consume it. No critical chance, multiplicative scaling, or bonus on every Skill. |

Hollow Crown and Ashen Fang follow the existing GDD relic examples. Ember Locket and Fractured Lens are provisional content additions selected to provide meaningful effects without depending on unimplemented Speed/Curse/Critical systems. Each relic has one fixed strength, no rarity tiers and no duplicate stacking. Effects support different action/resource needs; this avoids direct higher-tier replacements. These are conservative starting values, not a claim that long-term win rates or all possible future combinations have been balance-tested.

The shrine offers three distinct entries from the four-entry catalog using deterministic seeded ordering. Reloading the same uncommitted shrine does not reroll its choices. Only one is acquired. Current design includes one shrine per run; multiple shrines and handling fewer than three remaining unowned relics are future work.

## UI/UX

The established three-card reward panel, entrance animation, hover feedback and click lock are reused. Relic cards display RELIC and UNIQUE / ACTIVE RUN instead of upgrade tier/stack information. Descriptions are compact enough for the existing lower text area. The shrine uses the existing eye marker provisionally; bespoke four-relic illustrations remain future art work.

The stage-3 four-lane row uses centers -360, -120, 120 and 360 within the existing 1000-unit map viewport. The legend includes Relic. No scene rewiring or new Inspector assignment is required.

Figma layout specification: https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=5-2 . It uses a reusable card component and the established black/violet palette. Roboto is the Figma substitute; Unity keeps its installed Liberation Sans. No new Canva asset is claimed.

## Save compatibility and lifecycle

Relics are stored as validated unique active-run modifiers in the existing checkpoint. Corrupt identity, effect or stack values are rejected before replacing runtime state. Acquisition resolves the shrine and saves at the map boundary. Repeated selection callbacks cannot grant a second relic or advance twice. Restart clears the active build; battle-local Burn and first-Skill flags reset at battle entry. Relic effects require an active run.

RouteVersion 1 identifies the new graph. Missing/zero RouteVersion restores the previous graph without the added shrine, preserving old committed paths and future choices. Existing node and modifier enum values remain unchanged; new values are appended. Existing checkpoint schema/hash envelope remains intact.

## Validation

First full isolated Unity PlayMode run: **102/102 passed**, zero failures, duration 62.56 seconds. Six new tests cover the four-entry catalog, deterministic three-card offers, uniqueness, optional graph, legacy graph restore, actual card acquisition, state roundtrip, restart reset, Guard/HP caps, first successful Skill, Burn stacking/expiry/reset.

Final pass adds corrupt relic-stack restore rejection and rendered card containment/overflow checks at 1920×1080 and 1280×1024 after the final description/HUD refinement. Final result and visual evidence are recorded below after inspection.

Second full run: **102/102 passed**, zero failures, 62.41 seconds. Visual inspection then found dark inherited card text despite passing containment checks. Set the shared reward label color explicitly to pale white and replaced the stale background map log with a shrine-specific sentence. Added an assertion for label brightness. A third verification run covers this correction; its result is recorded below.

Standalone Windows build and manual complete-run playthrough are not part of this verification. Player balance acceptance remains separate from automated correctness.

### Verified final result

Third full PlayMode run: **102/102 passed**, zero failures/skips, **63.94 seconds**. No C# compiler errors were found in the final Unity log. The final 1920×1080 and 1280×1024 captures were visually inspected: pale text is legible, all three cards remain equal-sized and contained, and the Ashen Fang description fits. Twelve production/test file SHA-256 values match the isolated tested copies. No source edits followed this successful run.

Evidence: `first-results.xml`, `second-results.xml`, `verified-results.xml`, `source-hashes.csv`, `relic-cards-1920.png`, `relic-cards-1280.png`. Tests ran in the existing isolated SYNGRAVA_BattleTests project; player saves were not used. Functional implementation and automated verification of this four-relic milestone are complete. Placeholder glyph art, broader relic inventory UI, multiple shrines and long-term balance tuning remain explicitly outside this completed slice.
