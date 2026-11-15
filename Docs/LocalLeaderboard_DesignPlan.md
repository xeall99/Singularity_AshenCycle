# Local Leaderboard design plan

Date: 25 September 2026. This follows the verified Standard Run score model and uses the existing SYNGRAVA dark-violet UI language. It is a read-only projection of Run History, not a second save transaction.

## Data and ranking

- Only Standard Run records with score model 1 enter this table. Older `UNSCORED` history remains visible in Run History but is omitted from ranking.
- Rank by final score descending, then resolved depth descending, bosses defeated descending, then shorter active duration. If all gameplay values tie, earlier finish time and ordinal `RunId` provide a stable order. The table shows ten best records, never ten most recent.
- Each row shows rank, cleared/defeated, score, node depth, boss count, and duration. Selecting a row shows seed, finish date, ruleset, and point breakdown. Data comes from Run History; opening or selecting cannot mutate saves or the active run.
- A read failure shows an archive-unavailable state without deleting or rewriting data.
- Standard-only archives created before the mode/ruleset/cycle fields remain readable after new records are appended. Unity may serialize a previously missing mode string as an empty string; both forms identify the same legacy Standard record. Original scores remain unchanged.

## Layout

- Add a compact full-width **TOP RUNS** button below the Main Menu's four utility buttons. Keep Continue and New Expedition dominant; move the small version footer down to retain breathing room.
- The read-only `FATE LEDGER` overlay has a wide list with ten compact rows and a selection detail strip. Use the existing violet border, deep near-black surface, white text, violet for Cleared, and muted crimson for Defeated. Older unscored entries never appear as zero-score competitors.
- The panel scales within the Main Menu canvas and remains inside both 1920×1080 and 1280×1024 captures. Row labels and detail text must stay inside their bounds; button names remain stable for tests.
- Keyboard selection uses a dark violet fill with readable text. Up/down stays within the table and Close, right moves to Close, and Cancel returns focus to the Main Menu opener. Focusing a row updates its detail strip.

## Acceptance

Top-ten sorting and all tie-breaks are deterministic; records with the same RunId cannot appear twice because Run History already enforces identity. Read failures do not modify checkpoint or Echo data. UI opens and closes by mouse or keyboard focus. No package, scene, prefab, SQLite migration, or Inspector assignment is required.

Migration verification must append new results to an archive without the new metadata, reopen it, and retain every record. UI verification covers empty and unreadable archives, source-file byte equality before/after viewing, selection/Cancel behavior, and text containment at both capture resolutions.
