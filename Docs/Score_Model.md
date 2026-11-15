# Standard Run score model

Current implementation: **model 2**, corrected on 2 October 2026. Only Event Horizon and Reversal Orbit earn the capped Named Convergence bonus. Unstable Pulse still appears in combat statistics and grants its mana effect, but earns no score and does not consume the three-bonus limit. The node values below are unchanged.

Model 1 incorrectly included Unstable Pulse in that limit. Its archived scores remain readable with their original model number; the existing leaderboard filter ranks only the current model, so unlike scores are not mixed. No old result or Echo claim is rewritten.

Schema-3 active checkpoints now include an optional `ScoreModelVersion`. Current-model checkpoints retain both capped counters exactly. Older checkpoints remain playable and keep run identity, route, resources, modifiers, and total pattern statistics. Their old score-bonus counters reset to zero on restore because the per-battle Named/fallback split cannot be reconstructed; node points still derive from their committed route, and future Named patterns earn model-2 bonuses. Unsupported future score models and current-model counters that exceed the Named pattern total are rejected before runtime state is changed. The checkpoint envelope, schema number, SQLite schema, and ruleset remain unchanged.

Date: 24 September 2026. This plan follows the GDD's node and capped Convergence point baseline. The first implementation applies a 1.0 difficulty multiplier to Standard Run only. Infinite modes and Ranked validation are separate milestones.

## Rules

| Finished activity | Points | Limit |
| --- | ---: | --- |
| Normal Battle node | 100 | Once when the node resolves |
| Random Event node | 75 | Once after its choice commits |
| Elite Battle node | 500 | Once when the node resolves |
| Boss node | 1,500 | Once on Standard Run clear |
| Named Convergence | 50 | At most the first three in each resolved battle |

Rest, Merchant and Relic nodes give no direct score. Damage, healing, turn count, time, Embers, Echo and build strength give no points; these can be farmed or vary for reasons unrelated to route progress. A Convergence in a battle that ends in Defeat gives no bonus because that encounter did not resolve. The score is a sum of integer parts and is calculated from committed node types and a separately tracked capped Convergence count. No UI, filesystem, or random generator is used by the calculator.

## Persistence and compatibility

The run state records both total Convergences (for Run Summary) and score-eligible Named Convergences (for scoring). The current battle count resets when a combat node is resolved. `ScoreModelVersion` distinguishes current counters from earlier aggregate counters within schema 3; legacy bonus counters reset on restore rather than guessing an earlier battle breakdown. Each new finished Run History record stores the score and its breakdown with an explicit score-model version. Model-1 records retain their original score and model label in History but do not enter model-2 Top Runs. Records predating score support remain readable as unscored; no record receives a fabricated retroactive score.

Acceptance: repeated calculations are identical; four or more Named Convergences in one battle never award more than three bonuses; fallback triggers grant no points and do not consume that cap; separate resolved battles have independent caps; a failed node does not award its node or Convergence points; current-model save/restore preserves eligible counts; legacy progress/history remains readable; no existing battle, save, or menu interaction regresses.
