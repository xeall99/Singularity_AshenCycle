# Standard Run standalone continuation — 30 September 2026

Status: **one tool-controlled full Windows Clear verified; human release gate remains open**.

## Interactive evidence

The executable used for this playthrough was `Builds/Verification-2026-09-30-SaveFeedback/SYNGRAVA.exe`, at 1920×1080. Inputs were ordinary mouse actions through the Computer Use skill (`@oai/sky`), with a fresh screenshot inspected after each action. No debug combat, direct state mutation, or checkpoint injection was used. This is an interactive tool-controlled run and earns **zero** credit toward the required three human-controlled runs.

The compiled company/product identity was `CodexGateTests/SYNGRAVA_QualityGate_20260930`. Starting from Main Menu **NO SAVE / ECHO 0**, New Expedition wrote its checkpoint under `%USERPROFILE%/AppData/LocalLow/CodexGateTests/SYNGRAVA_QualityGate_20260930/Saves/`. This establishes save separation for this executable; the original player profile was not modified.

| Check | Observed result |
| --- | --- |
| Full Standard Run | **PASS (tool-controlled)**. Seed `1690253357`, RunId `a50e539d82164327a4eef13151786bde`, 10/10 nodes, Abyss Herald defeated. |
| Route | BATTLE → EVENT → RELIC → EVENT → BATTLE → BATTLE → ELITE → REST → MERCHANT → BOSS. Only connected choices were selected. |
| Ordinary battle/reward flow | Automatic rewards after non-final victories; temporary Vital Core stacked three times. Weakness reduced damage in the Elite encounter; Boss phase II and Curse were visible. No observed softlock. |
| Event/Relic/recovery | Gather Ash; Ember Locket; Silent Well Recover Focus; Rest Restore HP; Merchant mana purchase. Pre-Boss checkpoint recorded HP 121, Mana 7, Embers 59 and nine visited nodes. |
| Final Summary | **PASS**. Score 2,650, HP 84/145, Mana 5/10, Embers 89, Echo +124, battles won 5, damage 250, actions 15, Convergences 4, duration 12:09. Full seed and RunId visible without footer overlap. |
| Persistent result | **PASS**. One cleared History record with matching identity, score, resources and route; wallet balance 124 with one claim for that RunId. Active checkpoint and backup were absent after Clear. |
| Relaunch | **PASS**. Main Menu showed NO SAVE and ECHO 124. History showed one cleared 10/10 run, score 2,650, correct route/build. Top Runs showed rank #1, score 2,650, 10/10, one Boss, 12:09. |
| Alt+F4 after Summary | **PASS in this tool-controlled session**. Player process ended; log reached physics/input shutdown and CodeReloadManager cleanup without a crash signature. This does not resolve the earlier native-crash cause or replace human Alt+F4 verification. |
| Quit Game after relaunch | **PASS**. Player process ended and the relaunch log recorded normal shutdown. |

Evidence files: `clear-run-initial-checkpoint.json`, `clear-run-preboss-checkpoint.json`, `clear-run-history.json`, `clear-run-wallet.json`, `clear-run-Player.log`, and `relaunch-history-quit-Player.log`. Screenshots were visually inspected in the Computer Use tool output; these are not exported PNG evidence. Logs contain shutdown graphics-resource warnings, so a clean exit is not a claim of zero warnings.

## Concrete visual issue found

The Anomaly reward cards showed replacement squares in place of their symbols. `clear-run-Player.log` confirms unsupported Unicode U+2726, U+25C7 and U+2744 in `RewardGlyph`, replaced by U+25A1 in LiberationSans SDF. The catalog now uses C, B, F and I monograms, consistent with the existing letter portraits for rewards and relics. All four Anomaly definitions retain their codes, effects, benefits, consequences and values.

`TC_ANO_001A_AllAnomalyCardGlyphsRenderWithTheSceneFont` displays all four definitions across two card pages, checks the actual portrait font supports each character and produces a visible character mesh, and captures both pages at 1920×1080 and 1280×1024. Its shared capture helper still checks overflow and panel containment.

The initial suite returned 140 passed / 1 failed out of 141. The new glyph test passed; the existing end-to-end test expected its new cleared run to appear in Top Ten, but the reused test profile already contained 36 finished runs and ten higher-scoring entries. The fixture now starts that scenario with an empty History and backup after asserting the isolated test product identity. Production leaderboard filtering/sorting is unchanged. The initial failure is retained in `initial-history-fixture-results.xml` and `initial-history-fixture-Unity.log`; it is not counted as a passing regression.

The completed rerun is **141/141 PASS**, zero failed/skipped, 143.0888884 seconds, Unity exit 0 (`full-results.xml`, `unity-playmode.log`). All four Anomaly glyphs passed font support and visible-mesh checks. The four exported `anomaly-cards-page*-*.png` images were visually inspected; both resolutions were readable without overflow. `source-hashes.csv` verifies 52/52 source/scene/test/build-script hashes match the isolated project.

The post-fix Windows x86_64 Development Build is `Builds/Verification-2026-09-30-AnomalyGlyph/SYNGRAVA.exe`: **BUILD SUCCEEDED**, Unity exit 0, 0 errors, 498 warnings, 332,436,020 bytes, 90.00 seconds. The build log is `unity-build.log` in that folder. Its compiled `app.info` confirms `CodexGateTests/SYNGRAVA_AnomalyGlyphGate_20260930`; the full interactive Clear above used the earlier SaveFeedback build, not this post-fix executable. Executable SHA-256: `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`.

Post-fix interactive smoke test at 1920×1080: **PASS** for Main Menu, New Expedition → map, checkpoint write in the compiled test profile, Alt+F4 at the saved map boundary, relaunch → Continue enabled for stage 1/10, and Continue → map. Seed `-2134465660`, RunId `f10f2b2ba75a42f1bd60e3d9dc030d26`, HP 100, Mana 5, Embers 0, no modifiers or visited nodes. The serialized `StateJson` before closure and after Continue was identical (`postfix-initial-checkpoint.json`, `postfix-continued-checkpoint.json`). This checks the initial boundary only; it does not establish post-reward resume, corruption recovery, or a full post-fix expedition. Logs are `postfix-new-run-Player.log` and `postfix-continued-Player.log`.

## Remaining release gate

Three consecutive human-controlled Windows expeditions remain pending. Standalone natural Defeat, New Expedition temporary-build reset after Clear/Defeat, full keyboard/controller/resolution checks, and the standalone failed-save/retry interaction still need their own evidence. Continue/Recovery/Abandon have earlier evidence on RecoveryFix, not new verification on the post-glyph build. Battle character art is still represented by prototype rectangles, and Settings categories remain the documented shell where functionality has not been implemented. Infinite Casual remains on hold.

No scene, package, font asset, Inspector reference or save schema was changed in this continuation. Existing workspace changes were preserved. GDD design rules are unchanged.
