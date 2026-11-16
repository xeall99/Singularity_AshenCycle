# SYNGRAVA Project Context

Version 0.5
Updated 16 November 2026
Owner Akmal Maulana Ghani

## Progress continuation — skill audit and Infinite Run plan — 16 November 2026

- The skill collection/loadout feature is complete in runtime and already pushed to `origin/main`. The catalog contains 30 curated skills with 30 distinct active-effect IDs, 30 distinct passive-effect IDs, and 30 distinct passive descriptions. The follow-up copy audit marks all 29 one-shot passives as once per battle and Echo Script as repeatable.
- The skill implementation and passive-copy audit each passed three consecutive isolated Unity 6000.6.0f1 Play Mode runs, 168/168 tests per run with 0 failures and 0 skips. Evidence remains in `Docs/TestResults/2026-11-15-SkillSystem/REPORT.md` and `Docs/TestResults/2026-11-15-SkillFrequencyAudit/REPORT.md`. Numeric balance remains provisional until owner playtest.
- The next feature design is Infinite Casual. Its research plan was rechecked against Hopoo Games' primary Simulacrum video/patch notes on 16 November and updated in `Docs/Infinite_Run_DesignPlan.md`. The design remains unimplemented and must not alter Standard Run behavior.
- Standard Run has three owner-accepted consecutive clears on the recorded 8 November Windows candidate, plus two additional reported clears. Cold Continue, Recovery, Defeat, Abandon, and save retry are still not all evidenced on that same candidate; keep the Infinite implementation gate closed until those paths are verified and the checklist is updated.
- The 16 November change updates design/context documentation only. No skill runtime data, save data, Unity scene, or player profile was modified.

## Guided Tutorial and Field Guide — 14 November 2026

- The Main Menu now provides an optional **FIELD GUIDE**. It opens a six-page in-game reference for the Standard Run, turn actions, hidden intent/Analyze, Singularity Orbit/Convergence, route nodes/rewards, and checkpoint/history/Echo.
- The Field Guide is on-demand and replayable; the separate **guided first-run tutorial** automatically appears on an untouched Stage 1 map for a profile that has not completed or skipped it. The tutorial spotlights the actual reachable map node and game buttons, advances after real player actions, adapts to unavailable Mana actions and enemy-first turns, and provides **SKIP TUTORIAL** plus Escape. It does not pause time or grant resources. Completion is stored through `UserSettingsStore` without changing the SQLite schema.
- Previous/Next boundaries, Escape, Close, focus restoration, and page counter for the Field Guide are implemented in `Assets/Project/Scripts/UI/TutorialUI.cs`; `MainMenuUI` creates its utility entry in both generated and serialized-scene layouts. Guided overlay is built at runtime by `Assets/Project/Scripts/UI/GuidedTutorialController.cs` and connected to existing map/combat actions. No Inspector assignment, scene migration, package, or save-schema change is required.
- Design and acceptance plan: `Docs/Tutorial_DesignPlan.md`. It records the research, behavior, boundaries, and test plan for both the guided flow and replayable Field Guide.
- Historical Field Guide verification remains recorded in `Docs/TestResults/2026-11-14-Tutorial/REPORT.md`. Guided tutorial verification is separate: baseline 152/152 before implementation, then 158/158 in each of three consecutive Unity 6000.6.0f1 `BattleSnapshotTests` Play Mode runs (0 failed/skipped). XML and Unity logs are retained in `Docs/TestResults/2026-11-14-GuidedTutorial/REPORT.md` and its evidence files.
- Tests include first-run real-action progression, Skip/Escape persistence, early clear and defeat handling, low-Mana Skill/Analyze guidance, enemy-first turn handling, and layout containment. Two seed-sensitive existing combat assertions now calculate their expected critical damage from the active run seed. This is automated Play Mode evidence, not a human visual/readability review or player-learning study. Custom artwork and evaluation of learning outcomes remain future work; do not claim measured learning effectiveness.

## Skill collection and loadout — 15 November 2026

- Runtime contains 30 curated official skills. The player starts with Shadow Strike and can persist up to three unique owned skills in the local Skill Archive. Skill Archive opens from Echo Archive in Main Menu; changes apply to the next Expedition, while an active Run keeps its saved loadout snapshot.
- A Standard Run that has resolved at least one node offers one permanent skill after Clear or Defeat. The offer presents up to three unowned skills, is stable per RunId/milestone, can be deferred, and remains claimable from Skill Archive. Abandoned or zero-node Runs do not earn it. The reward does not spend Echo or increase Base Stats.
- Profile data lives in `Application.persistentDataPath/Database/player_progress.db`; custom Content Manager rows are excluded. Run snapshots now use `SeededRunState` schema 4. Schema 3 checkpoints migrate with Shadow Strike in slot 1 and two empty slots.
- On 15 November, the passive hooks were audited against the 30 authored descriptions and corrected where runtime outcomes differed. Burn/Ember, Burn/Curse, Guard-conditioned follow-ups, status duration extension, and hit/kill triggers now have explicit regression coverage. Unequipped passives do not consume their once-per-battle trigger.
- Every catalog row has a distinct non-empty `ActiveEffect`, `PassiveEffect`, and passive description; Echo Script intentionally repeats whenever immediately preceded by Guard. This validates unique identity, not final balance.
- Design, researched roster, and Infinite Run's next-feature plan: `Docs/Skill_Collection_Loadout_DesignPlan.md` and `Docs/Infinite_Run_DesignPlan.md`. The three final Unity Play Mode passes and their isolated profiles, XML, logs, and source hashes are recorded in `Docs/TestResults/2026-11-15-SkillSystem/REPORT.md`: each run passed 168/168 with 0 failures and 0 skips.
- Automated coverage includes catalog/passive identity, profile persistence, retry-safe claims, locked/duplicate loadout rejection, snapshot restore, duration extension, Burn/Ember, Burn/Curse, Guard-conditioned effects, and the full battle/run regression suite. Skill numbers remain provisional pending owner playtest and balance review; automated passes do not certify visual quality or balance.
- Follow-up UI-copy audit confirms all 29 one-shot passive tooltips say `each battle` or `once per battle`; Echo Script is the only passive marked repeatable. The catalog assertion and full Play Mode suite passed 168/168 in each of three further isolated save profiles, with baseline, three XML/log pairs, diagnostic attempt, and matching source hashes recorded in `Docs/TestResults/2026-11-15-SkillFrequencyAudit/REPORT.md`. No combat rule or value changed in that follow-up.

## Owner clarification — player skill progression, 3 October 2026

- The intended player feature is a curated, game-authored skill collection and loadout. At each 10-wave reward point in Infinite, Legend, or special runs, present three authored skill choices; the player selects one for their skill inventory, then configures which owned skills to bring in a loadout before starting a run. This is horizontal progression and must not increase locked Warden Base Stats.
- This is separate from the SQLite CRUD demonstration. The owner explicitly directed that Custom Skill CRUD not be exposed from the player-facing Main Menu, so players cannot author arbitrary skill values. Keep the repository and demo UI available for internal UKK/regression use only unless the owner changes this decision. Freeform custom rows must never enter combat, player collection, loadout, or wave rewards.
- As of 3 October, combat loaded only `SKL_SHADOW_STRIKE`; that historical status was superseded by the skill collection/loadout implementation recorded above. The Content Manager's `0–999` Power validation ceiling is a technical/demo limit only, not an approved gameplay balance rule.
- The first authored roster is a 15-skill draft in `Docs/Skill_Design_Catalog_Draft.md`. It includes provisional damage/Mana/passive values and external gameplay-pattern inspirations; it is not yet approved balance or runtime data.

## Work resumed — 26 October 2026

- **Custom Skill is removed from the player-facing Main Menu.** `MainMenuUI` no longer creates the Content Manager menu button or automatically attaches `ContentManagerUI`. The `MainMenu.unity` scene has no serialized Content Manager component or button reference. The content repository and panel remain available only for UKK/internal regression use; custom rows remain disconnected from combat and player progression.
- Regression source now verifies the boundary: `TC_MENU_001` expects no Content Manager button or component, and `TC_CRUD_001` attaches the panel explicitly inside the test before exercising SQLite CRUD. Both updated assemblies compile in an isolated .NET check: production source **0 errors / 60 warnings**, Play Mode test source **0 errors / 0 warnings**. This check used temporary copies of Unity-generated project files with only the unavailable VS Tools analyzer reference omitted; it is not a Unity Editor compile result.
- The initial 26 October isolated Play Mode attempt did not produce a result XML: Unity could not move temporary Library files inside the sandbox, Licensing Client startup failed, and Burst reported a missing compiler pipe. A successful isolated Play Mode retry, fresh Windows build, and standalone Main Menu visual smoke followed on 8 November; see `Docs/TestResults/2026-11-08-UnityRunnerRetry/REPORT.md`. Formal exact-build human Standard Run acceptance remains pending.
- The 15-skill catalog described at that time was a design draft. The current runtime has a 30-skill catalog, collection, loadout, and Standard Run Skill Memory; values still need balance review.
- **Next verification:** the Main Menu removal is covered by 151/151 Play Mode tests and a successful Windows build plus standalone visual smoke in the 8 November report. The next release-gate step is owner-run exact-build human acceptance. After the skill catalog is approved, define fixed loadout slots and duplicate-offer behavior before implementing authored catalog data and combat hooks.

## Verification update — 8 November 2026

- After the owner signed in to Unity Hub, the official isolated runner was retried outside the sandbox. Unity compiled scripts and completed **151/151 Play Mode tests**, with 0 failures and 0 skipped tests. The Main Menu separation test and internal CRUD regression test passed. XML, report, and source-copy hashes are recorded in `Docs/TestResults/2026-11-08-UnityRunnerRetry/`.
- The final log still contains entitlement lookup 404 and package/compiler warnings, but these did not prevent script compilation or the successful test result. Earlier sandboxed attempts remain recorded as failed-to-start diagnostics and are not counted as test runs.
- A fresh isolated Windows x86_64 Development build completed in Unity `6000.6.0f1`: **0 errors / 513 warnings**, 332,458,125 bytes, BuildPipeline 306.83 seconds. The executable is `Builds/Verification-2026-11-08-MenuRemoval/SYNGRAVA.exe`, with isolated identity `CodexGateTests/SYNGRAVA_MenuRemovalGate_20261108`; executable SHA-256 is `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`, gameplay assembly SHA-256 is `1878C94E5221CAC5073728047FC97D1FDB5904619C76DBA6EF1701E5B7B34AFE`. Full build log is in the same folder.
- Standalone Main Menu smoke at 1920×1080 confirmed the expected menu renders and has no Content Manager/Custom Skill entry; Continue correctly shows `NO SAVE`. The isolated player log contains no exception, missing-script, or crash entry, with one Direct3D diagnostic (`d3d12: failed to query info queue interface (0x80004002)`). Visual/build evidence and scope are in `Docs/TestResults/2026-11-08-UnityRunnerRetry/REPORT.md`.
- The exact-build human Standard Run acceptance remains pending. The latest Play Mode and Main Menu startup checks do not count as the required manual Standard Runs and transaction checks. Infinite Casual remains gated by the outstanding items in `Docs/StandardRun_ReleaseGate_Checklist.md`; a design plan alone does not open that gate.

## Manual acceptance follow-up — 13 November 2026

- The owner accepted Run 2 in the three-run Standard Run count even though Begin New Journey was selected from the preceding Summary; the owner played the complete run to Clear. Four Clear 10/10 results are now visible in Run History and Top Runs on the 8 November Windows candidate; scores, depth, boss count, and durations match the supplied Summary screens.
- After the fourth Clear, the candidate profile contains a checksum-valid fresh active checkpoint at Stage 1/10 with base HP/Mana, zero Embers, and no modifiers; Echo balance remains 513. This confirms the observed Clear-to-new-run reset and saved boundary. The owner left this checkpoint active.
- Manual cold Continue restoration, Recovery, Defeat, Abandon, and failed-save retry have not been rechecked on this exact candidate. The Standard Run clear-count acceptance is owner-accepted; keep these transaction checks visible before closing the wider Windows gate.

## Run Summary navigation build — 13 November 2026

- `RunSummaryUI` now creates **BEGIN NEW JOURNEY**, **MAIN MENU**, and **EXIT GAME** in its runtime footer. Main Menu loads the existing MainMenu scene and is unavailable until Echo and History writes succeed; Exit calls the appropriate player/editor quit API.
- A new Windows x86_64 Development build was produced in `Builds/Verification-2026-11-13-RunSummaryNavigation/` using the same isolated save identity as the 8 November candidate. Build result: **0 errors / 513 warnings**, 563.32 seconds. The gameplay assembly hash is `D56A7CC663442CC3945C1489E30E383B730A99762455CD11FEE133352953EF0A`.
- The old 8 November executable does not include these actions. The new build has compiled, but the Run Summary UI and Main Menu/Exit interactions still need inspection in a clear run. No Play Mode tests or manual UI tests were run for this change. See `Docs/TestResults/2026-11-13-RunSummaryNavigation/REPORT.md`.
## Implementation baseline — 3 October 2026 (historical)

- **SQLite CRUD demonstration remains internal, not player-facing.** The 3 October report's 151/151 pass predates the removal of the menu entry. The 8 November isolated regression verifies the updated state: `TC_MENU_001` asserts the player menu does not contain or initialize the panel, and `TC_CRUD_001` opens it only when explicitly attached inside the test. The content repository and demo panel remain available for UKK/internal regression, while custom rows remain disconnected from combat. At the time of this 3 October baseline the post-removal Windows build and visual review were pending; both were completed on 8 November as recorded above.
- Final Windows x86_64 Development build: `Builds/Verification-2026-10-03-ContentManagerFinal/SYNGRAVA.exe`, **0 errors / 515 warnings**, 332,458,390-byte player, BuildPipeline 818.39 seconds, isolated profile `CodexGateTests/SYNGRAVA_ContentManagerGate_20261003`. Gameplay assembly SHA-256 `4F5AAA632720F5CE3F5946546C93A9BF847FBB3BC5A1F58481A596D31809CBAB`. `source-hashes.csv` confirms all seven touched source/meta files match workspace, test copy, and build copy. Standalone UI created `UKK Test Ember` (Mana 3 / Power 24), updated it to Power 27 after confirmation, then on a full close/relaunch displayed Power 27 again. Read-only SQLite evidence is `Docs/TestResults/2026-10-03-ContentManager/standalone-db-readback.json`; final Player.log has no missing-script, exception, or crash entries.
- Settings is now functional in Main Menu and Battle. Gameplay rows state the locked rules (checkpoint after resolved node, enemy intent through Analyze); Accessibility toggles Reduce Motion for encounter/combat/Orbit presentation; Graphics cycles Windowed/Borderless and Unity quality presets; Audio cycles Master Volume in 25% steps. Preferences apply immediately and persist in a separate SQLite `Database/user_settings.db` under `Application.persistentDataPath`, independent of the content database. Main Menu Settings does not pause time or show the Battle gear.
- Battle Settings offers **SAVE & MAIN MENU**. At an open map boundary it writes and verifies the active checkpoint before loading Main Menu; failed writes keep the player in Battle with an explicit retry message. An entered node, battle, or unresolved reward cannot exit through this action, preserving the GDD safe-save boundary. A completed run checks pending Echo/History completion and terminal checkpoint cleanup first. The route map now keeps the gear above its full-screen overlay, after the first Windows smoke exposed that it was hidden.
- Final isolated Play Mode regression **149/149 PASS**, 0 failed/skipped, Unity 6000.6.0f1. A new raycast test confirms the map gear is the top click target. Fresh Windows x86_64 Development build: `Builds/Verification-2026-10-03-SettingsMenuFinal/SYNGRAVA.exe`, **0 errors / 514 warnings**, gameplay assembly SHA-256 `8EB166285794FC5EF7FE5541F1BA818384AA4742290676A32E48AC0BD24C038C`, profile `CodexGateTests/SYNGRAVA_SettingsMenuGate_20261002`. The earlier build with the covered gear is retained only as diagnostic evidence.
- Final player smoke at 1920x1080 confirmed preference reload, visible/clickable map gear, save before Main Menu, Continue restoring the same Stage-1 RunId/HP/Mana, and unchanged checkpoint hash after Continue. Midbattle SAVE & MAIN MENU was blocked with an explanatory message and left the checkpoint hash unchanged. Final Player.log recorded 0 missing scripts and 0 exception/crash messages. Evidence and limitations: `Docs/TestResults/2026-10-02-SettingsMenu/REPORT.md`. No Inspector/scene/package/art migration or commit/push. The wider Standard Run exact-build human acceptance gate remains separate; tutorial and final character art remain future work.

## Latest verified implementation — 2 October 2026

- Windows closeout 1–2 Oktober pada build AnomalyGlyph: failed checkpoint dan failed Retry mempertahankan warning/hash primary; successful Retry menyimpan depth-2 boundary dan menghilangkan warning; Continue memulihkan StateJson persis; Recovery atas primary terpotong mempromosikan backup valid persis. Semua memakai profil `CodexGateTests/SYNGRAVA_AnomalyGlyphGate_20260930`, bukan save pemain asli.
- Ekspedisi yang dilanjutkan melalui input Computer Use normal mencapai **Clear 10/10**, seed `-2134465660`, RunId `f10f2b2ba75a42f1bd60e3d9dc030d26`, Echo +138, HP 66/168, Mana 3/10, Embers 140. Setelah relaunch, History dan Top Runs menampilkan satu hasil, wallet satu klaim/saldo 138, Continue NO SAVE, primary/backup terminal hilang. New Expedition mereset base resources/modifier/statistik. Cancel Abandon mempertahankan hash; konfirmasi mengganti RunId dan menghapus Vital Core. Laporan/evidence: `Docs/TestResults/2026-10-01-StandardRunCloseout/REPORT.md`.
- Playthrough menemukan ketidaksesuaian dengan aturan skor yang sudah disetujui: Unstable Pulse ikut memperoleh bonus Named Convergence. **Model skor 2** kini hanya menghitung Event Horizon/Reversal Orbit, +50 dengan cap tiga per battle selesai. Fallback tetap memberi mana dan masuk statistik total, tanpa poin atau memakai cap. Hasil model-1 2.950 pada run di atas dipertahankan sebagai arsip asli; ekuivalen aturan model 2 adalah 2.750, bukan hasil baru yang dimainkan atau ditulis ke History.
- Riwayat model 1 tetap terbaca dengan versi aslinya dan tidak ikut Top Runs model 2. Checkpoint schema 3 memiliki field opsional `ScoreModelVersion`; restore model terkini menjaga cap persis, restore legacy menjaga identitas/jalur/resource/modifier/statistik tetapi mengosongkan bonus lama yang tidak dapat dipisahkan Named/fallback per battle. Node points tetap berasal dari jalur committed; Named berikutnya memberi bonus model 2. Future model/counter melebihi total Named ditolak sebelum mutasi runtime. Save envelope, schema/ruleset, SQLite, scene, paket dan Inspector tidak berubah. Penjelasan: `Docs/Score_Model.md`.
- Regresi terbaru **144/144 PASS**, 0 failed/skipped, 145.5732346 detik, Unity 6000.6.0f1 pada salinan proyek terisolasi. Tiga tes baru mencakup fallback/cap, legacy restore dan History model lama. Manifest setelah koreksi **52/52 SHA-256 cocok** dengan proyek tes. Percobaan awal sandbox tertahan Licensing Client dan retry bertabrakan dengan proses itu; hanya proses tes milik sesi ini yang dihentikan setelah verifikasi command line. Percobaan peluncuran itu bukan hasil tes. XML/log ada di folder closeout.
- Kandidat Windows setelah koreksi: `Builds/Verification-2026-10-02-NamedScore/SYNGRAVA.exe`, identitas `CodexGateTests/SYNGRAVA_NamedScoreGate_20261002`. **Build final PASS, 0 error / 514 warning**, Windows x86_64 Development, 1.091,26 detik, 332.422.990 byte. Fresh import terisolasi menghilangkan 16 missing-script message yang muncul pada percobaan build dari Library tes; log/hash percobaan itu tetap diarsipkan dan penyebab cache hanya inferensi. Gameplay assembly final SHA-256 `287ACD680FF796B40594E98A5DD95CF428A03329EB917C8129F26B359A1CE076`; bootstrap exe identik dengan build lama sehingga hash exe saja tidak cukup. Manifest input build dan pemeriksaan workspace akhir **52/52 cocok**. Tidak ada edit `.meta`/scene untuk rebuild.
- **Smoke final Windows PASS**, 1920×1080, seed `-1957354374`, RunId `fd3e623b77b74d4f8528ff3902ffd916`: legacy Continue Stage 1, History model 1 tetap terbaca, Top Runs 0 ranked, saldo Echo 138. Empat Attack memicu satu Unstable Pulse dengan 0 bonus; reward Vital Core I menyimpan model 2. Battle terhubung `NormalBattle_02_L0` dengan Attack/Attack/Skill/Attack memicu Event Horizon (+6 damage), lalu Vital Core I kedua menyimpan Stage 3, HP 52/130, Mana 9, Embers 28, satu bonus eligible. Alt+F4 → relaunch → Continue kembali ke map Stage 3 dengan StateJson sama persis; History/wallet payload tidak berubah. Log startup/smoke/Continue final 0 missing-script dan 0 exception/crash. Player uji ditutup, checkpoint valid disimpan. Ini smoke dua battle, bukan full Clear model 2 atau tiga run manusia. Bukti: `score-build-smoke-check.json` dan laporan closeout.
- Pemilik telah melaporkan tiga run manusia, Continue, Defeat, resolusi/input dan exit sebagai selesai (**USER-REPORTED PASS**). Seed/RunId/tanggal/executable sesi tersebut belum diberikan. Ini tidak dianggap tiga run tool atau bukti manusia memainkan kandidat setelah koreksi. Transaksi teknis sudah dilengkapi; gerbang rilis formal untuk build skor baru masih memerlukan penerimaan manusia yang menyebut executable. `Docs/StandardRun_ReleaseGate_Checklist.md` mencatat perbedaan tingkat bukti ini. Infinite Casual tetap menunggu gerbang rilis; Settings lengkap, tutorial, CRUD demonstrasi SQLite dan art final tetap pekerjaan berikutnya sesuai cakupan.
- Catatan September di bawah adalah sejarah, bukan status terbaru. Tidak ada commit/push, assignment Inspector, perubahan art, atau penggantian pekerjaan lokal pengguna dalam closeout ini. GDD tidak diubah karena koreksi mengembalikan aturan Named-only yang sudah disetujui.

## Historical verification — 30 September 2026

- Regresi terbaru **141/141 PASS**, 0 failed/skipped. Satu ekspedisi penuh melalui Computer Use pada Windows build SaveFeedback mencapai **Clear 10/10**, seed `1690253357`, RunId `a50e539d82164327a4eef13151786bde`, score 2.650, Echo +124. Summary, History, wallet dan Top Runs cocok setelah relaunch; checkpoint terminal dihapus. Alt+F4 setelah Summary dan Quit Game setelah relaunch menutup proses dengan normal pada sesi tool ini. Ini **bukan run manusia** dan belum menutup gate. Laporan: `Docs/TestResults/2026-09-30-StandaloneContinuation/REPORT.md`.
- Playthrough menemukan glyph Anomaly tidak didukung LiberationSans SDF, menghasilkan kotak pengganti. Catalog memakai monogram C/B/F/I seperti portrait huruf reward/relic yang sudah ada; efek dan save schema tetap sama. Tes render mencakup keempat Anomaly pada 1920x1080 dan 1280x1024. Fixture tes integrasi juga mengosongkan History profil tes sebelum skenario yang mengasumsikan run pertama masuk Top Ten, sehingga pengulangan suite tidak dipengaruhi arsip tes lama; logika ranking produk tidak berubah.
- Build terbaru setelah perbaikan glyph: `Builds/Verification-2026-09-30-AnomalyGlyph/SYNGRAVA.exe`, Windows x86_64 Development, **0 error, 498 warning**, profil `CodexGateTests/SYNGRAVA_AnomalyGlyphGate_20260930`. Smoke interaktif 1920x1080 membuktikan Main Menu → New Expedition → checkpoint awal → Alt+F4 → relaunch → Continue → map dengan StateJson identik dan save uji terpisah. Bukti full Clear di atas berasal dari SaveFeedback, bukan ekspedisi penuh pada build glyph terbaru.
- Regresi sebelumnya: **140/140 PASS**, 0 failed/skipped. Kegagalan checkpoint kini menampilkan peringatan **PROGRESS NOT SAVED** dan tombol **RETRY SAVE** pada footer map; retry hanya menulis boundary map yang sudah committed. Tes membuktikan failed retry mempertahankan peringatan, successful retry menghilangkannya, dan klik stale pada tombol tersembunyi tidak menulis checkpoint. Summary menampilkan seed dan RunId; layout/render pada 1920x1080 serta 1280x1024 diperiksa. Cleanup pause Settings yang sudah ada juga mendapat tes teardown. Bukti: `Docs/TestResults/2026-09-30-SaveFeedback/REPORT.md`.
- Windows x86_64 Development Build SaveFeedback `Builds/Verification-2026-09-30-SaveFeedback/SYNGRAVA.exe` berhasil dengan **0 error, 498 warning**. Identitas `CodexGateTests/SYNGRAVA_QualityGate_20260930` dikonfirmasi melalui `app.info`, dan lokasi save terpisah dibuktikan melalui New Expedition pada playthrough Windows. Bukti interaktif sebelumnya tetap berlaku pada build RecoveryFix: Continue, Recovery, Abandon, dan Quit Game tercatat di `Docs/TestResults/2026-09-30-WindowsGate/REPORT.md`. Enam ekspedisi dengan combat normal serta kekalahan alami tetap tercakup dalam suite otomatis; hasil tersebut bukan run manusia.
- Gerbang produk masih terbuka: tiga Standard Run lengkap berturut-turut yang dikendalikan manusia, validasi akhir Clear/Defeat dan History/Top Runs, pemeriksaan resolusi/input di executable, dan cek Alt+F4 manual. Satu percobaan Alt+F4 melalui automasi mencatat crash native pada build sebelumnya; penyebab belum dipastikan. Infinite Casual tetap menunggu daftar gerbang manual. Opsi kategori Settings masih merupakan shell/prototype sesuai catatan historis; jangan menganggap audio/graphics/accessibility lengkap dari tampilan menu saja.

- Main Menu, Run History, Standard Run ScoreCalculator, dan Local Leaderboard top 10 tersedia. Tombol **TOP RUNS** membuka **FATE LEDGER** dari Main Menu.
- Tes integrasi Main Menu → Continue → Clear → Summary/History/Top Runs → New Expedition serta Abandon/temporary-build reset: **2/2 PASS**. Regresi Unity Play Mode: **135/135 PASS**, 0 failed, 0 skipped. Laporan: `Docs/TestResults/2026-09-26-StandardRunGate/REPORT.md`.
- Windows x86_64 Development Build **berhasil**, 0 error, 514 warning, di `Builds/Verification-2026-09-26/SYNGRAVA.exe`. Startup batchmode mencapai inisialisasi engine tanpa exception yang tercatat, tetapi tampilan dan input belum diverifikasi secara interaktif.
- Gerbang produk **belum selesai**: masih perlu tiga Standard Run manual berturut-turut tanpa blocker serta pemeriksaan interaktif Continue/Recovery, Clear/Defeat/Abandon pada build. Ikuti `Docs/StandardRun_ReleaseGate_Checklist.md` sebelum Infinite Casual.
- Pernyataan milestone lama di bagian historis adalah rekam progres, bukan status terbaru. Riwayat keputusan tetap dipertahankan.

## Purpose

This file is the machine-readable project context for Codex and other coding assistants working inside the local Unity repository. It summarizes approved design decisions, the current prototype, known differences between the prototype and the target design, and the rules for making safe changes.

This file provides persistent instructions and project memory. It does not train model weights. A future chat should read `AGENTS.md`, this file, the newest relevant source files, and the GDD before implementing a feature.

## Product Summary

SYNGRAVA is a single-player, turn-based dark-fantasy roguelite RPG for Windows. The player controls one active Warden, travels through a node-based run, gains temporary upgrades, and creates Convergence effects by arranging three combat actions in the Singularity Orbit.

The project is an SMK RPL UKK project built by one developer. Completion, stable database behavior, demonstrable OOP, readable UI, testing evidence, and a working Windows build have higher priority than optional online or content-heavy systems.

## Product Identity

| Field | Approved Value |
| --- | --- |
| Active title | SYNGRAVA |
| Previous working title | Singularity Ashen Cycle |
| Genre | Turn-based dark-fantasy roguelite RPG |
| Player format | One active Warden |
| Engine | Unity 6.6 `6000.6.0f1` |
| Render direction | Universal 2D first, staged 2.5D presentation later |
| Target platform | Windows PC 64-bit |
| Database | SQLite offline |
| Visual palette | Black, purple, and white |
| Core mechanic | Three-slot Singularity Orbit and Convergence |
| Mandatory mode | Standard Run |

## Scope Order

### Must Complete

- Main Menu, profile, Standard Run, node map, battle, reward, boss, and Run Summary.
- Locked Warden Base Stats and resettable Temporary Run Stats.
- Attack, Skill, Guard, mana validation, and enemy AI.
- Hidden enemy plan and Analyze.
- Singularity Orbit with at least two Named Patterns and Unstable Pulse fallback.
- SQLite content data, save data, at least one complete CRUD demonstration, and a Windows build.

### Complete After the Standard Run Is Stable

- Additional Named Patterns, Anomalies, relics, elite nodes, events, tooltips, tutorial, and accessibility polish.
- Infinite Run Casual, checkpoint resume, deterministic scoring, and a local top-ten leaderboard.

### Optional or Post UKK

- Infinite Run Ranked, authoritative global leaderboard, official seeded sessions, daily or weekly seeds, Memory Echo, and advanced Orbit manipulation.

### Excluded from the Current Scope

- Multiplayer, gacha, open world, large party combat, many Acts, full voice acting, live-service seasons, and a mandatory custom online backend.

## Locked Stats and Run Progression

Warden Base Stats belong to a ruleset and are not increased by character levels or permanent attribute points. Permanent progression unlocks skills, relics, Archive entries, presets, and other choices. It must not create a permanent raw-stat advantage at the start of a run.

Temporary Run Stats are modifier records owned by the active run. A modifier records its source, effect type, flat value, percent value, stack count, and scope. Effective values must be calculated from Base Stats plus active modifiers by one shared calculator.

Baseline formula:

`Effective Stat = (Base Stat + total flat bonuses) * (1 + total percentage bonuses)`

Round only at the final step defined by the stat calculator. Battle execution, UI preview, save restoration, Run Summary, and score validation must use the same calculation rules.

### Standard Run Stacking

- A Standard or Act run contains approximately 8 to 10 nodes.
- Eligible rewards can stack during that active run.
- The stack exists only until the run is Cleared, Defeated, or Abandoned.
- Starting a new run creates an empty run modifier collection.
- A reward must never overwrite the locked Base Stats record.

### Infinite Run Stacking

- Temporary upgrades may continue stacking without a fixed numerical stack cap during the active Infinite Run.
- Unlimited duration does not mean unlimited reward quality. Rarity remains controlled by depth eligibility and weighted chance.
- A high tier becoming eligible does not guarantee that it appears.
- When the Warden loses or abandons the Infinite Run, every Temporary Run Stat resets to the locked baseline.
- Only the final score, depth, build summary, history, and valid horizontal unlocks may persist.

## Battle Baseline

The current prototype uses these known values. They are implementation evidence, not final balance promises.

| Item | Current Prototype Value |
| --- | --- |
| Player Max HP | 100 |
| Player Max Mana | 10 |
| Starting Mana | 5 |
| Basic Attack damage | 10 in the current `BattleManager` prototype |
| Shadow Strike | 2 Mana and 20 Power from SQLite data |
| Guard | Reduces the next incoming damage by 50 percent and restores 2 Mana |
| Turn Mana recovery | 1 Mana after the enemy sequence in the current prototype |
| Current encounter count | Three sequential single-enemy battles |

The GDD ruleset table may contain later target values such as Attack, Defense, Speed, and Critical Chance. Do not silently mix those future domain-model values into the current prototype. Introduce a migration only when the battle model is ready.

## Singularity Orbit Contract

Orbit stores a maximum of three successful player action categories:

- `Attack`
- `Skill`
- `Guard`

Analyze, Item, failed input, cancelled target selection, and a skill rejected for insufficient Mana do not enter Orbit.

After the third successful action:

1. Resolve the full three-action sequence exactly once.
2. Apply the normal effect of the third action.
3. Apply the Convergence result exactly once.
4. Show the result through battle text and visual feedback.
5. Clear the three gameplay slots.
6. Continue the turn sequence only if battle has not ended.

Baseline implemented patterns:

| Sequence | Result | Prototype Effect |
| --- | --- | --- |
| Attack Attack Skill | Event Horizon | Third Skill gains 30 percent bonus damage |
| Guard Attack Guard | Reversal Orbit | Prepares a counter worth 50 percent of Basic Attack |
| Other sequence | Unstable Pulse | Restores 1 Mana, limited by Max Mana |

Arcane Collapse and Stable Core remain later content unless their code and tests already exist.

## Hover Only Convergence Preview

The preview must not display three permanent lines in the middle of the battle screen.

Required behavior:

1. Keep the preview hidden when fewer than two Orbit slots are occupied.
2. When two slots are occupied, hover or keyboard focus on Attack, Skill, or Guard requests a preview for that candidate third action.
3. Show one compact tooltip directly above the focused button.
4. Display the resulting Convergence name and a short numerical effect description.
5. Use `OrbitResolver.Preview` or the same pure resolver used by execution.
6. Hide the tooltip on pointer exit, focus loss, button click, enemy turn, modal opening, victory, defeat, or disabled input.
7. Analyze has no Convergence preview because it does not enter Orbit.
8. If Skill is disabled because Mana is insufficient, its hover feedback may explain the missing Mana but must not imply that the action can be executed.

## Hidden Enemy Plan and Analyze

Enemy intent is hidden during normal play. The game may prepare an action internally, but its type, damage, target, and effect are not shown before the player spends Analyze.

### Approved Analyze Option A

- Cost: `ceil(0.75 * EffectiveMaxMana)`.
- Example: with 10 Max Mana, Analyze costs 8 Mana.
- Limit: once per node.
- Turn use: Analyze does not consume the player's turn.
- Orbit: Analyze never adds, removes, or replaces an Orbit action.
- Result: after Analyze succeeds, the player may still choose Attack, Skill, or Guard.
- Reset: the once-per-node flag resets only when a new node begins, not every player turn.

### Analyze Validation Order

1. Confirm the battle is active and player input is permitted.
2. Confirm the current node has not already used Analyze.
3. Calculate the cost from Effective Max Mana and round upward.
4. Confirm current Mana is at least the calculated cost.
5. Deduct the Mana exactly once.
6. Mark Analyze as used for the node.
7. Reveal the already-prepared enemy action.
8. Keep the state in the player's action phase.

If any validation fails, Mana, turn, Orbit, and enemy plan remain unchanged.

### Stable Enemy Plan

The enemy action must be prepared once before the player chooses a combat action. Analyze reads the same immutable plan that the enemy later executes. The AI must not roll a new action after Analyze, because that would make the revealed information false.

Recommended data object:

`EnemyActionPlan { actionType, sourceEnemyId, targetId, baseDamage, hitCount, statusEffect, isLocked, planToken }`

For the current single-enemy prototype, Analyze can display that enemy's next action and estimated damage. For a future group encounter, it should display the total estimated incoming damage and a compact list of important non-damage effects.

### Baseline Enemy Actions

| Action | Meaning | Reveal Content |
| --- | --- | --- |
| Normal Attack | Standard damage using the enemy baseline | Name, target, hit count, and estimated damage |
| Heavy Attack | Higher damage with a clear internal multiplier | Name, target, hit count, and estimated damage |
| Charge | Prepares a stronger later action instead of normal immediate damage | Charge state and the action it prepares when known |

Exact Heavy Attack and Charge multipliers remain configurable balance values. Do not hard-code undocumented final numbers into several scripts.

### UI Migration Rule

The scene currently has an assigned `EnemyIntentText`. During the incremental migration, keep its serialized reference so the Inspector does not lose data. Clear or hide it by default, then reuse it as the temporary Analyze result display if practical. Rename or replace it only with an explicit scene migration plan, using `FormerlySerializedAs` when needed.

## Automatic Reward Selection

After a non-final victory, the reward flow starts automatically. The separate Choose Reward click is removed.

### Transition Sequence

1. Lock Attack, Skill, Guard, and Analyze input as soon as victory is resolved.
2. Show a brief victory state for approximately 0.8 seconds.
3. Hide or visually suppress battle commands and Orbit information that would overlap the reward screen.
4. Activate a full-screen black dim layer with approximately 70 percent opacity.
5. Generate three eligible, non-duplicate reward choices.
6. Position the three portrait cards below the visible screen.
7. Animate them upward into the center with a smooth eased motion and a short stagger.
8. Enable selection only after each card has reached a safe interactive state.
9. Apply one selected reward exactly once.
10. Save the completed reward transaction, close the modal, then continue to the next node or battle.

Use unscaled time for the modal animation if gameplay time is paused.

### Card Layout

Each reward card is a tall portrait panel rather than a wide rectangular button. It contains:

- illustration area or clear placeholder image;
- tier or rarity label;
- reward name;
- short mechanical description;
- exact numerical value;
- current stack count when the reward can stack;
- visible hover and keyboard focus state.

The visual direction uses original black, purple, and white assets with dark high-fantasy ornament and orbit motifs. Other games are references for hierarchy and usability only. Do not copy their artwork, typography, card frames, or layout pixel for pixel.

### Card Interaction

- Hover lifts and slightly scales the card.
- The border animates with a controlled purple orbit or traveling highlight.
- Pointer exit returns the card smoothly to its original position.
- One click locks all three cards before applying the reward.
- Repeated clicks cannot grant duplicate rewards or skip more than one node.
- Keyboard or controller focus must trigger the same information and border state.

## Reward Tier and Depth Rules

The reward system is seeded and weighted. Depth unlocks eligibility; a weighted roll determines the actual tier. The following names and thresholds are provisional tuning data and should live in configurable data rather than repeated conditionals.

| Tier | First Eligible Depth | Design Meaning |
| --- | ---: | --- |
| Tier 1 | 1 | Basic build foundation |
| Tier 2 | 5 | Early specialization |
| Tier 3 | 15 | Stronger focused modifier |
| Tier 4 | 30 | Build-defining combination support |
| Tier 5 | 60 | Mid-depth rare modifier |
| Tier 6 | 120 | Advanced Infinite modifier |
| Tier 7 | 250 | Deep-run modifier |
| Tier 8 | 500 | Very rare deep-run modifier |
| Tier 9 | 750 | Extreme-depth modifier |
| Tier 10 | 1001 | Highest tier becomes eligible, never guaranteed |

Standard Run normally reaches only early tiers because it ends after approximately ten nodes. Infinite Run can reach later eligibility thresholds while retaining low-tier results in the weighted pool. This prevents every late reward from becoming automatically top tier.

Each rarity should eventually have visually distinct original cards. The ten-tier content set is a long-term target. The UKK build may use fewer fully implemented tiers as long as the data structure already supports expansion and the scope is documented honestly.

## Save and Reset Rules

| Run State | Persistent Result | Temporary Build |
| --- | --- | --- |
| Active | Checkpoint, node, seed, current resources, modifiers | Restored exactly on Continue |
| Cleared | History, score, valid unlocks, summary | Deleted after the completion transaction |
| Defeated | History, depth, score, summary | Deleted before starting another run |
| Abandoned | Abandon record when required | Deleted and cannot leak into a new run |
| Corrupt | Recover last complete checkpoint or start clean | Unvalidated modifiers are never applied |

Checkpoint after a reward is selected and fully applied. Do not save a half-open reward selection as if the transaction were complete.

## Infinite Run and Leaderboards

Infinite Casual reuses the Standard Run combat, reward, node, save, and content systems. A cycle contains approximately ten nodes and ends with a boss. Defeating the boss starts the next cycle. Difficulty and eligible rewards increase from data-driven rules.

Infinite mode begins only after:

1. three consecutive Standard Runs complete without blocker errors;
2. Temporary Run Stats reset correctly after victory, defeat, and abandon;
3. Active Run save and resume restores the same state;
4. the score calculator is deterministic.

The local leaderboard stores at least the best ten results by mode, including score, depth, cycle, boss count, seed, duration, ruleset version, and date.

Global Ranked is optional or post UKK. A trustworthy global leaderboard requires authentication, official session or seed data, ruleset version validation, plausible-value checks, and server-side acceptance. A client must not be trusted merely because it uploads an integer score.

## Current Scene and Inspector Context

The observed Battle scene contains this structure:

```text
Battle
  Main Camera
  BattleManager
  Canvas
    Background
    PlayerBox
    EnemyBox
    PlayerHPText
    PlayerManaText
    EnemyHPText
    BattleLogText
    EnemyIntentText
    CommandPanel
      BasicAttackButton
      SkillButton
      GuardButton
      AnalyzeButton
    OrbitPanel
      OrbitRings
      OrbitSlot1Text
      OrbitSlot2Text
      OrbitSlot3Text
      ConvergencePreviewText
    ResultPanel
      ResultTitleText
      ResultDescriptionText
      RetryButton
      NextBattleButton
    RewardPanel
      RewardTitleText
      RewardButton1
      RewardButton2
      RewardButton3
  GameDatabase
  EventSystem
```

Names are evidence from the prototype scene. Inspect the actual Hierarchy before relying on them because the user may have made newer local changes.

## Known Runtime APIs

Preserve these current relationships unless the change updates every caller safely:

- `GameDatabase.Instance.IsReady`
- `GameDatabase.Instance.GetSkillByCode("SKL_SHADOW_STRIKE")`
- `GameDatabase.Instance.GetEnemyByBattleIndex(battleNumber)`
- `SkillData.Name`, `SkillData.ManaCost`, `SkillData.Power`
- `EnemyData.Name`, `EnemyData.MaxHP`, `EnemyData.AttackDamage`, `EnemyData.RewardEmbers`, `EnemyData.IsBoss`
- `OrbitState.AddAction`, `OrbitState.IsFull`, `OrbitState.Actions`, `OrbitState.Clear`, `OrbitState.Count`, `OrbitState.TryGetAction`
- `OrbitResolver.Resolve` and `OrbitResolver.Preview`
- `BattleVisuals.PlayPlayerAttack`, `PlayEnemyAttack`, `PlayEnemyHit`, `PlayPlayerHit`, and `PlayGuard`

Do not redefine existing enums or data classes in `BattleManager.cs`. Search the repository before adding a type.

## Current Implementation Status

### Working Prototype Evidence

- SQLite skill and enemy data can be read.
- Basic Attack, Shadow Strike, Guard, enemy turn, victory, defeat, and three sequential battles have worked in Play Mode.
- Orbit stores action categories and resolves Event Horizon, Reversal Orbit, and Unstable Pulse.
- Orbit text slots, procedural rings, and basic placeholder animation have appeared in Play Mode.
- Three temporary-stat reward choices have appeared and can affect the current run prototype.
- A Git repository and initial commit exist locally.
- `AnalyzeButton` exists in the Battle scene Hierarchy.
- EnemyActionPlan, hidden intent, and Analyze are implemented and verified by 23 automated Unity Play Mode cases on 12 September 2026. See the verification record below; this does not mark all Battle UX v0.4 features complete.
- Hover/focus Convergence preview, automatic post-stage rewards, and portrait reward cards are implemented and verified together with Analyze by 29 automated Unity Play Mode cases on 12 September 2026. See the current Battle UX verification record below.
- Seeded weighted reward tiers, no-duplicate reward generation, and explicit stack presentation are implemented and verified together with the earlier Battle UX behavior by 31 automated Unity Play Mode cases on 12 September 2026.
- Resource carryover between nodes is implemented and verified with all earlier Battle UX behavior by 40 automated Unity Play Mode cases on 12 September 2026. Current HP/Mana survive reward selection; only a new run or Restart Run initializes resources.
- The approved reward/Orbit follow-up and `RunModifierCollection` are implemented and verified with all earlier behavior by 42 automated Unity Play Mode cases on 13 September 2026.

### Implemented in Battle UX Version 0.4

- Attack, Skill, and Guard show one hover/focus-only Convergence tooltip above the relevant command when Orbit contains exactly two actions.
- A non-final victory opens the reward selection automatically after a short unscaled delay; no Choose Reward input is exposed.
- The three existing reward buttons are presented as portrait cards with a black translucent dim layer, staggered entrance, exact reward/stack labels, and pointer/focus border feedback.
- `RewardManager` creates one three-choice snapshot per cleared node from distinct `RewardDefinition` entries. Tier eligibility follows the GDD depth thresholds, and the same snapshot drives card text, glyphs, exact values, and effect application.

### Planned Next

- Authored portrait art to replace the current procedural Singularity card illustrations.
- `RunStatController`, followed by seeded `RunManager` state and the Standard Run node map.

### Known Technical Debt and Design Differences

- `BattleManager` currently owns too many responsibilities. `RunModifierCollection` now owns active modifier records, while the existing private aggregate fields remain synchronized for compatibility. Continue extraction incrementally.
- The reward pool currently contains the three verified stat effects. Generation and tier selection are data-driven and seeded, but relics, Anomalies, and a larger database-backed reward pool remain planned.
- Enemy intent is now hidden by default and Analyze reveals an immutable Normal Attack snapshot. Heavy Attack, Charge, and multi-enemy execution remain planned.
- Convergence preview is hover/focus-only and uses `OrbitResolver.Preview`; the former permanent multi-line text in the Orbit panel is no longer displayed.
- `OrbitUIAnimator` still detects ordinary slot text changes, while completed Convergence now sends an explicit pulse before Orbit clears.
- The procedural Orbit ring graphic is a functional placeholder and still needs visual polish.
- Reward portraits use procedural Singularity rings and letter glyphs because no authored reward portrait assets exist yet.
- The GDD defines tier thresholds but does not define numeric weights or value multipliers. `RewardManager.CreateDefaultTierConfigurations` contains provisional centralized balancing values; they must move to content data when the production reward repository is introduced.
- `battleNumber` is the current depth fallback and the run seed exists only for the active runtime session. `RunManager`, checkpoint persistence, and same-seed resume are not implemented yet.
- The current battle prototype supports one enemy at a time. Group Analyze output is a future-compatible requirement, not proof of current group combat.
- `StartBattle` carries current HP/Mana between nodes and clamps them to their current maximum. Following the user's 13 September decision, Vital Core now adds its exact flat value to both Effective Max HP and current HP. This state still belongs to the live Battle scene; cross-scene persistence, Rest rules, and checkpoint restore await the run systems.
- Reward card labels are resolved separately from portrait glyph text and include inactive children safely.
- Reward persistence/save-after-selection is not implemented because save/checkpoint work remains outside this Battle UX slice.
- Full Infinite generation, resume, local leaderboard, Ranked validation, and global leaderboard are designs, not completed features.

## File Ownership Direction

| Responsibility | Preferred Owner |
| --- | --- |
| Battle phase and results | `BattleManager` initially, later a focused battle state controller |
| Action animation timing | `BattleVisuals` and later `ActionSequencer` |
| Three action memory | `OrbitState` |
| Convergence result | `OrbitResolver` |
| Orbit presentation | `OrbitUIAnimator` and `SingularityOrbitRings` |
| Prepared enemy action | `EnemyActionPlan` and `EnemyAI` |
| Analyze validation | Focused Analyze controller or BattleManager integration during prototype |
| Reward generation | `RewardManager` |
| Card transition and hover | `RewardSelectionUI` and `RewardCardView` |
| Run modifiers | `RunStatController` or `RunModifierCollection` |
| Run lifecycle | `RunManager` |
| Save and recovery | `SaveService` and repositories |
| Score | Pure `ScoreCalculator` |

Avoid a large rewrite solely to match these target names. Introduce classes when their responsibility is actually being extracted and tested.

## Acceptance Tests for Battle UX Version 0.4

### Analyze

- With Max Mana 10 and Mana 8, Analyze spends 8 Mana, reveals the prepared action, and leaves player action buttons available.
- With Mana 7, Analyze is rejected without changing Mana, turn, Orbit, or the prepared enemy plan.
- A second Analyze in the same node is rejected without cost.
- Analyze does not fill any Orbit slot.
- A new node resets the Analyze limit.
- The enemy executes the same plan that Analyze revealed.

### Hover Preview

- At Orbit count 0 or 1, no Convergence tooltip appears.
- At Orbit count 2, each valid action shows only its own result while hovered or focused.
- The tooltip is positioned above the correct button and remains within the Canvas.
- Pointer exit and focus loss hide it.
- The actual third-action result matches the preview.

### Reward Modal

- A non-final victory opens three rewards automatically after the configured delay.
- No Choose Reward input is required.
- The dim background is black and remains partially transparent.
- Three different cards enter from below with smooth staggered motion.
- Hover and focus animate the relevant card border.
- Battle input is blocked while the modal is active.
- One selection applies exactly once, saves after completion, and advances exactly one node.
- Final boss victory opens Run Summary instead of an ordinary reward.
- A reward screen contains three different codes, remains stable when presentation refreshes, and regenerates deterministically from the same seed and depth.
- Tier 10 is ineligible at depth 1000, becomes eligible at depth 1001, and is never guaranteed by eligibility alone.

### Regression

- Attack, Skill, Guard, Mana, enemy turn, victory, defeat, Retry, Restart Run, Orbit reset, and all Inspector references still work.
- Unity Console contains no red error on the tested flow.
- The same flow works in a Windows development build before the milestone is marked complete.

## Codex Working Procedure

Before edits, Codex must inspect the repository and state the smallest plan. It must preserve unrelated changes, serialized fields, `.meta` files, scene references, and database data. It must not claim Unity compilation or Play Mode success unless Unity actually performed that check.

After edits, Codex must report exact files changed, Inspector assignments, automated checks, remaining Unity tests, known limitations, and a suggested small commit message. Accepted decisions and implementation status should then be recorded here and in the GDD change log.

## Enemy Action Snapshot Verification 12 September 2026

### Implemented Behavior

- `EnemyActionPlan` is a plain immutable C# class. It copies enemy ID and AttackDamage into a Normal Attack plan with target Warden, one hit, multiplier 1, and no additional effect.
- `StartBattle` clears the previous plan and Analyze result, resets the node-use flag, then prepares one plan before player input. The current deterministic Normal Attack repeats that same object throughout the node. Analyze, Attack, Skill, Guard, and EnemyTurn do not prepare or reroll it.
- `enemyIntentText` remains assigned. It is cleared and hidden in Awake/start, then displays action, target, hits, estimated base damage, and effect after Analyze. Its existing text rectangle grows vertically at runtime to fit the result. Enemy execution, battle end, and errors hide the result again.
- Analyze deducts `ceil(0.75 * EffectiveMaxMana)` once, keeps the player's turn, and never changes Orbit. Until the Effective Stats system exists, `playerMaxMana` is the documented fallback. Integer calculation `M - M / 4` is exact for non-negative integer M. Max Mana 10 costs 8: Mana 8 becomes 0, Mana 7 remains 7 on rejection, and Mana 10 becomes 2.
- EnemyTurn reads `TotalEstimatedDamage` from the saved snapshot. Guard modifies received damage only; Reversal counter behavior is unchanged. The estimate is base incoming damage before Guard.
- Missing/invalid plans stop battle with an explicit BATTLE ERROR and Restart Run action. There is no silent replacement plan. Missing-plan Analyze does not charge Mana.
- `EndBattle` ignores repeated completion calls so Embers/results are not applied twice. Existing reward selection and resource-reset behavior are retained.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **23 passed, 0 failed, 0 skipped**. Unity exited with code 0. Gameplay and test source files in the tested copy matched the repository byte-for-byte at verification time.

The suite loads the actual Battle scene in an isolated copy of the project, uses the already-resolved package sources, and gives the copy a separate company/product identity to protect the user's runtime database. Tests invoke real button listeners or command methods, inspect runtime state, and let enemy coroutines execute. Time scale is accelerated for automation; mouse navigation and visual polish were not manually tested.

| Test | Result | Evidence checked |
| --- | --- | --- |
| TC-ANL-001 | PASS | Mana 8 becomes 0; prepared plan visible; Analyze disabled; turn/Orbit unchanged |
| TC-ANL-002 | PASS | Mana 7 rejected with required message; state/plan unchanged; retry succeeds after Mana reaches 8 |
| TC-ANL-003 | PASS | Repeated use cannot charge again or replace the snapshot, even after Mana is replenished |
| TC-ANL-004 | PASS | Changing EnemyData.AttackDamage after reveal does not change actual base damage; subsequent turn retains the same snapshot |
| TC-ANL-005 | PASS | Victory/reward advances one node, creates a new plan, resets Analyze, and clears the previous reveal |
| TC-ANL-006 | PASS | Two existing Attack entries remain identical; no third Orbit entry or Convergence |
| TC-ANL-007 | PASS | Waiting after Analyze does not start enemy action; valid Attack/Skill/Guard remain available |
| Eight cost cases | PASS | Upward rounding including Max Mana 10, 11, 12 and integer maximum |
| Battle/Orbit regression | PASS | Attack, Skill cost/rejection, input lock, Guard, enemy damage, Reversal, Event Horizon/preview, Unstable Pulse, victory, defeat, Restart Run, next battle, and final Run Complete |
| Missing-plan cases | PASS | Two deliberately injected errors are explicitly handled without reroll; expected error logs are asserted |
| Reference/immutability checks | PASS | Snapshot properties have no setters; all serialized Unity object references remain assigned; Basic Attack stays 10 |
| Manual visual/input review | NOT RUN | Automated tests verify text/state and listeners, not mouse hit areas or visual composition |
| Windows standalone development build | NOT RUN | This task verified Editor compilation and Play Mode; standalone remains a release gate |

No unexpected Error/Exception logs were accepted by the Play Mode suite. Existing package warnings occurred during the initial import. The test API deprecation warning was corrected before the final passing run.

Reproduce from the repository root with `./Tools/Run-BattleSnapshotTests.ps1`. It creates a temporary project and prints the Unity log and XML paths. The suite intentionally skips execution under the production product name because GameDatabase currently overwrites its runtime database from the seed in Editor.

The retained final report is `Docs/TestResults/EnemyActionPlan-PlayMode.xml` (repository-root-relative). Its timestamps are UTC and individual test records contain runtime evidence.

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs` and this context file at `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md`.
- Added: `Assets/Project/Scripts/Battle/EnemyActionPlan.cs` and its `.meta`.
- Added: `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, `Syngrava.Battle.PlayModeTests.asmdef`, and the corresponding asset/folder metadata.
- Added: `Tools/Run-BattleSnapshotTests.ps1` and `Docs/TestResults/EnemyActionPlan-PlayMode.xml`.
- The Battle scene was not edited by this milestone. All 33 existing serialized fields and their names were preserved. `analyzeButton` and `enemyIntentText` already reference the existing objects; no Inspector assignment or model component attachment is required.
- Existing local Battle scene/Orbit changes were preserved. Additional TMP fallback-font and solution-file changes observed during the session were left intact, not reverted. No package, SQLite schema, or GDD design change was made.

### Remaining Scope

This completes the EnemyActionPlan/Hidden Intent/Analyze implementation and automated verification slice. It does not complete all Battle UX v0.4 or replace the Windows-build/manual-review gate.

Normal Attack is the only executable plan type; no multi-hit, Heavy Attack, Charge, status system, or multi-enemy behavior is claimed. Node identity is still the prototype battle index. At this verification point StartBattle reset resources; the Resource Carryover verification below supersedes that limitation. Analyze becomes unused on a new node but remains disabled until Mana is sufficient.

The hover/reward/card follow-up named here was completed later on the same date; the current record follows below. Save, node map, Infinite, and leaderboards remain planned.

## Battle UX Version 0.4 Verification 12 September 2026

### Implemented Behavior

- `BattleCommandMenu` keeps its existing Pointer Enter/Exit, Select/Deselect, and press trigger path. It publishes one preview request or hide signal; `BattleManager` subscribes once and remains the only owner of `OrbitResolver.Preview`. Pointer and keyboard focus are tracked separately so leaving a hovered button does not discard a still-focused command.
- The existing serialized `convergencePreviewText` reference is retained and reparented only at runtime into `ConvergenceHoverTooltip`. The tooltip is hidden at Orbit counts 0 and 1. At count 2 it shows only the hovered/focused Attack, Skill, or Guard result, follows that button, clamps to the Canvas, ignores raycasts, and clears on exit, focus loss, action execution, turn change, battle result, or reward modal.
- `UpdateOrbitUI` no longer writes permanent `ORBIT n/3` or all-three preview lines into the center of the Orbit panel. The three slot references remain unchanged. `OrbitUIAnimator` now recognizes the em dash empty marker and receives an explicit Convergence pulse before the Orbit state clears.
- A non-final victory keeps the Victory result visible for 0.8 real-time seconds, then opens rewards automatically. `nextBattleButton` and its listener remain assigned for compatibility, but the button is hidden and no Choose Reward click is required. Final victory continues directly to Run Complete.
- Reward selection uses the existing panel, title, and three button references. The modal background is black at 72% alpha. The three buttons become 280x410 portrait cards and enter from below with staggered unscaled animation. All reward input remains locked until the entrance completes; keyboard/gamepad focus then starts on the first card.
- Each card has a procedural Singularity illustration, a stable A/V/G glyph, Tier I name, exact effect value, and current-to-next stack count. Pointer hover and UI focus lift/scale the card and brighten its border. Authored portrait sprites are still a future art pass.
- `rewardSelectionCommitted` and the existing end-battle guard ensure multiple callbacks cannot apply more than one reward or advance more than one node. Closing the modal clears its UI focus, restores command/Orbit presentation for the new node, and leaves Analyze/node snapshot reset behavior intact.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **29 passed, 0 failed, 0 skipped** in 14.63 seconds. Unity exited with code 0. The Battle scene and all production/test source files listed below matched the repository byte-for-byte in the isolated test project.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, insufficient Mana, one-use limit, deterministic execution, node reset, unchanged Orbit, and unchanged turn |
| Analyze rounding matrix | PASS | Eight integer cases, including Max Mana 10 -> cost 8 and `int.MaxValue` without overflow |
| TC-HVR-001 | PASS | Orbit 0/1 never reveals Convergence preview |
| TC-HVR-002 | PASS | Attack/Skill/Guard each show only their own resolver result; tooltip stays above its button and inside Canvas; exit hides it |
| TC-HVR-003 | PASS | Pointer/focus overlap restores the focused preview; focus loss hides it; executed Event Horizon matches the preview |
| TC-RWD-001 | PASS | Non-final Victory displays first, reward opens automatically, Choose Reward stays hidden, battle UI is blocked/hidden, dim layer is black 72%, and cards start below Canvas while locked |
| TC-RWD-002 | PASS | Three distinct portrait cards, stable glyph/label separation, Tier/value/stack text, completed entrance, first-card controller focus, pointer hover, focus lift, border feedback, and reset |
| TC-RWD-003 | PASS | Two rapid callbacks apply Attack reward once and advance exactly one node |
| Battle/Orbit regression | PASS | Attack, Skill, Guard, Mana, enemy plan/damage, Reversal, Event Horizon, Unstable Pulse, victory, defeat, Restart Run, automatic node advance, and final Run Complete |
| Missing-plan and reference checks | PASS | Expected snapshot errors remain explicit; no silent reroll; all serialized Unity object references remain assigned; snapshot remains immutable; Basic Attack remains 10 |
| Unexpected Console errors/exceptions | PASS | No unexpected matching error, exception, or compiler-error entries; the two deliberately injected missing-plan errors were expected and asserted |
| Manual visual/mouse review in an interactive Editor | NOT RUN | Position, state, pointer/focus dispatch, and animation end states were automated; final art composition still needs human visual review |
| Windows standalone development build | NOT RUN | Editor compile and Play Mode passed; a standalone development build remains a release gate |
| Save-after-reward persistence | NOT RUN | Save/checkpoint infrastructure is not part of this milestone and remains planned |

Reproduce from the repository root with `./Tools/Run-BattleSnapshotTests.ps1`. The retained current report is `Docs/TestResults/BattleUX-v0.4-PlayMode.xml`. The earlier 23-case snapshot report remains in place as historical evidence.

### Files and Inspector

- Modified for this slice: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Project/Scripts/UI/BattleCommand.cs`, `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added for this slice: `Assets/Project/Scripts/Battle/RewardSelectionUI.cs`, `Assets/Project/Scripts/Battle/RewardCardView.cs`, their `.meta` files, and `Docs/TestResults/BattleUX-v0.4-PlayMode.xml`.
- The Battle scene was not edited by this slice. Existing local scene changes were preserved and the current scene file was the exact one loaded by the passing suite.
- No Inspector assignment is required. `convergencePreviewText`, `nextBattleButton`, `rewardPanel`, `rewardTitleText`, and all three reward buttons keep their existing serialized references. Runtime presentation components are added idempotently to those existing objects.
- No package manifest, SQLite schema/database, base-stat value, GDD rule, or existing `.meta` file was changed.

### Remaining Scope

This completes the requested hover preview, automatic reward transition, and portrait-card presentation slice in Editor Play Mode. It does not claim authored reward art, weighted reward generation, persistence, Windows build validation, or completion of the wider run systems.

The weighted tier and resource carryover slices named above were completed later on the same date; see their verification records below. Heavy Attack, Charge, multi-enemy, node map, save/checkpoint, Infinite Run, and leaderboard remain planned.

## Weighted Reward Tier Verification 12 September 2026

### Implemented Behavior

- `RewardDefinition` is an immutable reward snapshot containing code, name, description, glyph, effect type, tier, eligibility depth, roll weight, exact flat/percent value, and presentation text. UI and effect application read the same object.
- `RewardManager` centralizes all ten GDD tier thresholds: nodes 1, 5, 15, 30, 60, 120, 250, 500, 750, and 1001. Eligibility expands the weighted pool and never guarantees the highest eligible tier.
- The GDD does not specify numeric tier weights or value multipliers. The manager therefore owns one provisional configuration table instead of scattering balancing literals through battle/UI code. Tier I retains the existing Inspector baselines: Attack +10%, Max HP +15, and Guard Strength +20%.
- A run seed and battle depth deterministically generate one three-choice snapshot. The three reward codes are unique, their order is seeded, and repeated UI configuration does not generate new choices.
- The existing three button listeners remain registered exactly once. Their legacy private callback names now select snapshot indices, so randomized card order still applies the displayed effect. A stale rapid click returns before inspecting a cleared snapshot.
- Stack counters are stored separately per reward effect. Card labels show tier, name, exact numeric effect, description, and current-to-next stack count. Restart Run clears all reward modifiers and counters.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **31 passed, 0 failed, 0 skipped** in 16.76 seconds. Unity exited with code 0. All six production/test source files involved in this slice matched the isolated test project byte-for-byte.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, snapshot execution, node reset, unchanged Orbit, and unchanged turn |
| TC-HVR-001 through TC-HVR-003 | PASS | Hidden preview at Orbit 0/1, per-command preview at Orbit 2, pointer/focus lifecycle, and resolver parity |
| TC-RWD-001 through TC-RWD-003 | PASS | Automatic modal, input lock, portrait/card animation, dynamic reward data, rapid-click guard, one application, one node advance, and next-screen stack presentation |
| TC-RWD-005 | PASS | Tier 10 absent from eligibility at depth 1000, eligible at 1001, possible in seeded rolls, and not guaranteed |
| Seed/no-duplicate snapshot | PASS | Same seed/depth produces the same ordered codes and tiers; all three codes differ; presentation refresh keeps the same snapshot object |
| Battle/Orbit regression | PASS | Attack, Skill, Guard, Mana, enemy snapshot/damage, Reversal, Event Horizon, Unstable Pulse, victory, defeat, Restart Run, reward advance, and Run Complete |
| Serialized references and duplicate types | PASS | Existing Inspector references remain assigned; no duplicate reward/enemy/orbit class or enum; Basic Attack remains 10 |
| Unexpected Console errors/exceptions | PASS | No unexpected errors or exceptions were accepted by the final Play Mode run |
| Windows Development Build | PASS | Unity built `MainMenu.unity` and `Battle.unity` for Windows x86_64 with Development enabled; BuildResult Succeeded, 0 errors, output size 214,971,951 bytes |
| Interactive visual review | NOT RUN | Automated tests cover layout state and input events; authored art and final human composition review remain pending |
| Save/checkpoint restore | NOT RUN | The current session seed, reward stacks, and modifier values are not persisted because `RunManager` and checkpoint repositories remain planned |

The final Play Mode report is retained at `Docs/TestResults/RewardTier-v0.4-PlayMode.xml`. The Windows build was produced only in the isolated temporary project so no build artifact or temporary Editor script was added to this repository.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RewardDefinition.cs`, `RewardManager.cs`, and their `.meta` files.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `RewardSelectionUI.cs`, `RewardCardView.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- The Battle scene and all existing serialized fields were preserved. No new scene component, Inspector assignment, package, database schema, or GDD rule change is required.
- Existing `attackRewardPercent`, `maxHPRewardAmount`, and `guardRewardPercent` fields remain the Tier I baseline. All runtime card objects and glyphs are still created idempotently.

### Remaining Scope and Next Milestone

The current reward pool has three stat effects and procedural portraits. Relics, benefit-and-consequence Anomalies, authored card art, checkpoint save, and database-backed reward content remain planned. The provisional weights and multipliers require balancing data before production content lock.

The resource carryover and `RunModifierCollection` follow-ups were completed later, as recorded below. The next smallest milestone is `RunStatController`, then seeded `RunManager` state, before node map and checkpoint integration.

## Resource Carryover Verification 12 September 2026

### Approved Rule and Implemented Behavior

- The early prototype baseline in the GDD starts a battle at 5/10 Mana, while the later run-state contract carries current resources between nodes. Following the context authority order, 5/10 starting Mana and full HP now apply only to a new run or Restart Run. No GDD design rule or balancing value was rewritten.
- `Start` enters through the existing `RestartRun` path after database validation. That path clears temporary rewards/stacks, initializes a fresh reward seed, then sets HP to current maximum and Mana to the configured starting value clamped within Max Mana.
- `StartBattle` preserves current HP/Mana and only clamps out-of-range values. Resource initialization no longer runs after reward selection. The existing commit guard still applies a reward once and advances exactly one node.
- At this verification point Vital Core increased Effective Max HP without adding current HP. The user's 13 September decision supersedes that behavior; see the current follow-up record below. Current Mana carryover remains unchanged.
- Every new node still clears Orbit, prepares a new enemy snapshot, hides intent, and resets Analyze usage. Mana 0 remains insufficient for Analyze until recovered through an existing valid resource action.
- Basic Attack remains 10. Max HP 100, Max Mana 10, starting Mana 5, Skill cost, Guard recovery, enemy-turn recovery, and Convergence effects retain their existing baselines. `playerMaxMana` remains the documented effective-maximum fallback until a run-stat controller exists.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **40 passed, 0 failed, 0 skipped** in 26.17 seconds; Unity exited with code 0. This includes nine new resource cases and all 31 prior battle/reward cases. The tests load the actual Battle scene in an isolated copy with a separate product/company identity to protect the user's runtime database. BattleManager, the test source, and Battle.unity matched the tested copy byte-for-byte.

| Test | Result | Evidence checked |
| --- | --- | --- |
| TC-RES-001 | PASS | Only a new run initializes full HP/starting Mana; node entry preserves 43 HP and 8 Mana with matching UI |
| TC-RES-002 | PASS | All three reward effects preserve spent HP/Mana; Mana 0/7/10, rapid selection, new plan, hidden intent, reset Orbit/Analyze, button availability, and no delayed refill |
| TC-RES-003 | PASS | Historical 12 September behavior increased capacity without healing; superseded by the 13 September Vital Core rule below |
| TC-RES-004 | PASS | Skill victory preserves its Mana cost and Unstable Pulse recovery into the next node; Guard and completed enemy-turn recovery still work |
| TC-RES-005, three cases | PASS | Out-of-range HP/Mana clamp to their bounds while valid low resources remain unchanged |
| TC-RES-006, two cases | PASS | Restart clamps configured starting Mana below zero or above Max Mana without changing the Max Mana stat |
| TC-ANL-001 | PASS | Mana 8 pays 8 and reveals the stored plan |
| TC-ANL-002 | PASS | Mana 7 rejects Analyze without state changes |
| TC-ANL-003 | PASS | Repeated Analyze never charges or replaces the snapshot |
| TC-ANL-004 | PASS | Enemy execution uses the revealed snapshot even when source enemy data changes |
| TC-ANL-005 | PASS | Reward advances once, resets Analyze, preserves spent Mana at 0, and requires sufficient Mana before Analyze is available again |
| TC-ANL-006 | PASS | Analyze leaves two Orbit actions unchanged |
| TC-ANL-007 | PASS | Analyze leaves the player's turn and valid commands available |
| Battle, Orbit, hover, and reward regression | PASS | Attack, Skill, Guard, recovery, three Convergences, victory, defeat, retry/restart, final victory, preview parity, card transition, and seeded reward selection |
| Inspector compatibility and unexpected Console errors | PASS | All 33 serialized fields retain their definitions; assigned references stay valid; no duplicate types/listeners introduced; no unexpected test errors or exceptions |
| Windows Development Build | PASS | Current source built MainMenu and Battle for Windows x86_64 with Development enabled; Succeeded, 0 errors, 510 warnings, 214,971,862 bytes; Unity exited with code 0 |
| Interactive Editor/standalone playthrough | NOT RUN | Automated Play Mode covers controlled node transitions; manual input, visual review, and an unassisted full run are not claimed |
| Save/reload and full Standard Run | NOT RUN | Persistent run state and the complete 8-10 node Standard Run are not implemented in this prototype |

Report: `Docs/TestResults/ResourceCarryover-v0.4-PlayMode.xml`.

The Windows build and its temporary Editor helper were produced only in the isolated test copy. The 510 build warnings remain recorded; this is not a warning-free build. No interactive standalone playthrough is claimed.

### Files and Inspector

- Modified in this slice: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added: `Docs/TestResults/ResourceCarryover-v0.4-PlayMode.xml`. No runtime class, field, enum, or listener was added.
- Battle.unity, its existing references, and the checked script/scene `.meta` files were preserved. Analyze is already connected; no Inspector or Hierarchy setup is required.
- Existing unrelated Git changes were preserved. No package, database, generated folder, or project setting was edited in the repository.

### Known Limitations and Next Milestone

Carryover is currently in-memory within the Battle scene. Scene reload still starts a new run; future RunManager/save integration must own continuity across scenes. There is no new Rest/healing reward, max-Mana modifier system, status-effect lifecycle, or balance adjustment. Automated tests use controlled HP/enemy HP to exercise transitions and do not certify whole-run difficulty.

The repository's default EditorBuildSettings still references the obsolete SampleScene path. Build verification must explicitly include MainMenu and Battle until a separate build-configuration task updates that list. Building a player does not prove a manual standalone playthrough.

`RunModifierCollection` was completed in the follow-up below. Continue with `RunStatController`, followed by seeded `RunManager` state. Preserve the current serialized fields and public API during each extraction. Node map, checkpoints, Infinite Run, leaderboard, authored portrait art, and broader content remain planned.

## Reward, Orbit, and Run Modifier Follow-up 13 September 2026

### Approved Behavior

- Vital Core applies one immutable reward snapshot value to two resources. `Max HP +N | HP +N` adds `N` to the active run's Effective Max HP and adds the same `N` to current HP, capped by the new maximum. Example: 64/100 with a +15 card becomes 79/115. The existing reward commit guard prevents rapid clicks from applying either increase twice.
- Reward cards have no automatic initial focus, so opening the modal does not glow a card before pointer or navigation focus reaches it. Pointer and focus requests are coordinated across all three cards; activating one card immediately clears the other two visual states.
- OrbitPanel remains the same scene object with the same three assigned TMP references. It is now anchored left of PlayerBox, with slot 1, 2, and 3 arranged vertically from top to bottom. The existing procedural rings are rotated into a vertical frame. Orbit recording and Convergence resolution are unchanged.
- `RunModifierCollection` owns immutable active-run modifier records by reward source. Each record stores source code, effect type, accumulated flat/percent value, stack count, and `ActiveRun` scope. Repeated rewards stack into the same source record. Effective values, card stack counts, and summary output read the collection; Restart Run clears it.
- The six existing private aggregate/stack fields remain present as synchronized compatibility projections. All 33 serialized fields, scene references, public APIs, and nine button listener registrations remain unchanged.

### Node Status

After a non-final victory, reward selection commits once, increments `battleNumber`, and `StartBattle` loads the next enemy by `BattleIndex`. The seed database currently supports the existing three sequential single-enemy battles, with the last battle acting as the prototype run boss/completion. A selectable node map and the complete Standard Run path of approximately 8-10 nodes have not been implemented. They remain dependent on `RunStatController` and seeded `RunManager` state.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Automated Play Mode: **42 passed, 0 failed, 0 skipped** in 26.74 seconds; Unity exited with code 0. Windows x86_64 Development Build using MainMenu and Battle: **PASS**, 0 errors, 494 warnings, 214,976,367 bytes. The production scripts, scene, test source, and new `.meta` in the isolated run matched the repository byte-for-byte.

| Test | Result | Evidence checked |
| --- | --- | --- |
| TC-RWD-002 | PASS | No initial selected/glowing card; resting borders/scales; pointer and keyboard focus each highlight exactly one card; switching cards clears the previous state |
| TC-RES-002 | PASS | All rewards retain node carryover; Vital Core adds the same exact flat value to current and maximum HP; rapid selection applies it once |
| TC-RES-003 | PASS | Two Vital Core selections add current and maximum HP on both nodes, retain locked base Max HP, and reset all temporary values on Restart Run |
| TC-RUN-001 | PASS | Three reward effects create three active-run source records; totals and compatibility fields agree; repeated Vital Core stacks one record; reset empties the collection |
| TC-ORB-001 | PASS | Orbit panel is entirely left of PlayerBox and the three active slot references form a descending vertical column |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, immutable enemy snapshot, new-node reset, unchanged Orbit, and unchanged player turn |
| Full battle/UX regression | PASS | Attack, Skill, Guard, Mana, enemy execution, all Convergences, hover preview, automatic reward, victory, defeat, retry/restart, carryover, and three-battle completion |
| Serialized references, listeners, duplicate types, and unexpected test errors | PASS | 33 serialized fields and 9 add/remove listener pairs retained; no duplicate modifier/enemy/orbit class or enum; no unexpected test errors/exceptions |
| Windows Development Build | PASS | MainMenu and Battle built for Windows x86_64 with Development enabled; Unity reported Succeeded and 0 errors |
| Manual mouse/visual review in an interactive build | NOT RUN | Automated input events and numeric layout constraints passed; final human inspection of glow and composition is still recommended |
| Selectable node map and full 8-10 node Standard Run | NOT RUN | Only three sequential database battles currently exist |

Report: `Docs/TestResults/RewardOrbitFix-v0.4-PlayMode.xml`.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RunModifierCollection.cs`, its `.meta`, and `Docs/TestResults/RewardOrbitFix-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `RewardManager.cs`, `RewardSelectionUI.cs`, `RewardCardView.cs`, `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No Inspector assignment is required. OrbitPanel, OrbitSlot1Text, OrbitSlot2Text, OrbitSlot3Text, reward buttons, and every prior serialized reference keep their existing file IDs and connections.
- No package, SQLite schema/database, locked Base Stat, or existing `.meta` file was changed.

### Next Milestone

Implement `RunStatController` as the shared calculator over locked Base Stats and `RunModifierCollection`. After that, move run depth, seed, current HP/Mana, and modifier ownership into a seeded `RunManager`; then implement the complete Standard Run node map.

## Authored Orbit Frame and Action Icon Follow-up 13 September 2026

### Approved Behavior

- The vertical Orbit layout remains to the left of PlayerBox. Its procedural ring component and GameObject are preserved for compatibility, but the component is disabled so it cannot overlap the authored frame.
- `SyngravaOrbitFrame.png` supplies the three rough square slots and surrounding eye/shard ornament. The texture has real transparency, uses a black-purple-white palette, ignores raycasts, preserves aspect ratio, and renders behind the slot content.
- Orbit slots no longer display font glyphs. The three existing `orbitSlot1Text`, `orbitSlot2Text`, and `orbitSlot3Text` objects, serialized fields, and file IDs remain active and continue to store the compatibility labels `A`, `S`, `G`, or the empty marker. Their TMP render components are disabled at runtime.
- Three centered, non-raycast `Image` children render authored action symbols. Attack uses a radial singularity strike, Guard uses an eye-crested shield, and the current `SKL_SHADOW_STRIKE` skill uses its own crescent-blade symbol. An unmapped skill uses a neutral vortex fallback instead of a misleading hard-coded Shadow Strike image.
- `BattleManager.RecordOrbitAction` passes the recorded slot index and exact skill code to `OrbitUIAnimator`. The animator keeps skill identity per slot and resolves it through a serialized code-to-sprite catalog. Adding a later skill only requires its sprite and catalog entry; `OrbitState`, `PlayerActionType`, and `OrbitResolver` remain unchanged.
- Slot icon and legacy TMP state are synchronized by the presentation API itself. Clearing Orbit removes all three sprites and labels together. Analyze still does not enter Orbit, and Convergence still clears Orbit after the third valid Attack, Skill, or Guard.
- The rejected Rubik Dirt Orbit font assets and their obsolete test report were removed because this approved direction uses symbols.

### Verification Results

Unity 6000.6.0f1 compilation: **PASS**. Final automated Play Mode: **43 passed, 0 failed, 0 skipped** in 26.84 seconds. Windows x86_64 Development Build of MainMenu and Battle: **PASS**, Succeeded with 0 errors, 499 warnings, and output size 224,150,223 bytes.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ORB-001 | PASS | OrbitPanel remains entirely left of PlayerBox; the three preserved TMP slot objects remain active and vertically ordered |
| TC-ORB-002 | PASS | Authored frame is first in render order; procedural rings are disabled; all icon Images are centered, upright, preserve aspect, ignore raycasts, and begin empty |
| Action icon mapping | PASS | Attack resolves its own sprite; `SKL_SHADOW_STRIKE` resolves the Shadow Strike sprite; an unknown skill resolves the generic skill sprite; Guard resolves the shield |
| Slot reset | PASS | Clearing Orbit disables all icon renderers, removes their sprites, and restores the internal empty labels |
| Texture import | PASS | Frame and all four icon PNGs import as single UI sprites with transparency and mipmaps disabled |
| Offscreen visual capture | PASS | Imported scene was rendered at 1600x900 after animation completion; all three symbols appeared within the matching frame squares and no legacy letters were rendered |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, deterministic enemy snapshot, node reset, unchanged Orbit, and unchanged player turn remain intact |
| Full battle/UX regression | PASS | Attack, Shadow Strike cost, Guard, Mana recovery, enemy execution, Convergences, hover preview, rewards, carryover, victory, defeat, restart, and final completion |
| Serialized references and unexpected Console errors | PASS | Existing TMP and battle references remain assigned; no duplicate class, enum, field, or listener was introduced; no unexpected error or exception was accepted |
| Windows Development Build | PASS | Final source and imported icons built for Windows x86_64 with Development enabled; Unity reported Succeeded and 0 errors |
| Interactive standalone playthrough | NOT RUN | Automated Play Mode, offscreen visual rendering, and player build passed; a human-controlled full standalone run is not claimed |
| Complete Standard Run node map | NOT RUN | The prototype still advances through the three existing database battles; selectable 8-10 node routing remains planned |

Report: `Docs/TestResults/OrbitActionIcons-v0.4-PlayMode.xml`.

The final offscreen screenshot and Windows player were generated only in the isolated temporary test project. No capture helper, executable, or build folder was added to the repository.

### Files and Inspector

- Added: `Assets/Project/Art/UI/Orbit/SyngravaOrbitFrame.png`, four action icon PNGs under `Assets/Project/Art/UI/Orbit/Icons/`, their Unity metadata, and `Docs/TestResults/OrbitActionIcons-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Battle.unity now contains `OrbitFrameArtwork`, `OrbitSlot1Icon`, `OrbitSlot2Icon`, and `OrbitSlot3Icon`. All sprite fields and the `SKL_SHADOW_STRIKE` catalog entry are already assigned. No manual Inspector setup is required for the current build.
- The old Orbit TMP objects, procedural `OrbitRings` object, `convergencePreviewText`, battle buttons, reward references, and all earlier serialized fields remain present and connected.
- No package, database schema, locked Base Stat, Analyze rule, reward value, or Convergence rule was changed.

### Known Limitations and Next Milestone

Shadow Strike is the only current skill with an authored dedicated Orbit sprite. Every future skill should add one square transparent sprite and one catalog entry using its stable skill code; until then it deliberately uses the generic skill icon.

The complete Standard Run node graph, save/checkpoint ownership, Heavy Attack and Charge enemy plans, multi-enemy combat, authored reward portraits, Infinite Run, and leaderboard remain planned. Continue with `RunStatController`, then seeded `RunManager` state, before implementing selectable Standard Run nodes.

## Orbit Runtime Visibility and Stage Entrance Follow-up 13 September 2026

### Corrected Behavior

- A live Editor report showed that `OrbitState` reached two actions and the Convergence hover preview reacted correctly, while all three action images remained invisible. The earlier visual capture had invoked `OrbitUIAnimator.SetSlotAction` directly, so it did not prove the real button-to-battle-manager presentation path.
- `BattleManager` now resolves the existing `OrbitUIAnimator` through the preserved slot hierarchy and, if that cached path is unavailable, finds the existing inactive scene component. It does not create a second component. Every Orbit UI update explicitly synchronizes the presentation from the authoritative `OrbitState` list.
- `OrbitUIAnimator` keeps the direct action/skill-code API, retains legacy hidden TMP labels, and adds a self-repair check. If an action remains in `OrbitState` but its `Image`, sprite, or post-animation alpha is unexpectedly hidden, the matching authored icon is restored without changing Orbit gameplay data.
- The three icon objects are direct children of `OrbitPanel`, after `OrbitFrameArtwork` in sibling/render order. Each icon is 118 x 118 while each compatibility slot is 84 x 84, so the rough symbol extends slightly past the inner square and overlays the border. The icon remains centered, upright, aspect-preserving, transparent, and non-raycast.
- Every `StartBattle` plays the Orbit panel entrance after battle chrome becomes active. The complete frame and slots begin 440 UI units to the left, fade in, travel to a subtle 14-unit overshoot, and settle at the authored position in 0.85 seconds using unscaled time. This runs on the first battle and after each completed reward transition into the next database battle.
- Orbit still records only successful Attack, Skill, and Guard actions. Analyze still spends no turn and records no Orbit action. Three valid actions still resolve one Convergence and clear all slots. No damage, mana, reward, snapshot, or node-progression rule changed.

### Verification Results

Unity 6000.6.0f1 compilation and full isolated Play Mode regression: **PASS**. Final suite: **45 passed, 0 failed, 0 skipped** in 29.30 seconds. Windows x86_64 Development Build of MainMenu and Battle: **PASS**, Succeeded with 0 errors, 500 warnings, and output size 224,154,475 bytes.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-ORB-001 | PASS | Orbit remains a vertical dropdown entirely to the left of PlayerBox |
| TC-ORB-002 | PASS | All icon objects are direct OrbitPanel overlay children, render after the frame, exceed the 84 x 84 compatibility slots, preserve aspect, ignore raycasts, and start empty |
| TC-ORB-003 | PASS | Two real `basicAttackButton.onClick` calls produce and retain icons in slots 1 and 2 across both enemy turns; an intentionally disabled slot is restored from `OrbitState` |
| TC-ORB-004 | PASS | The full panel starts left of its resting position with reduced alpha, then returns exactly to the authored position and alpha 1 after the stage entrance |
| Runtime visual capture | PASS | A 1600 x 900 render after two real Attack button actions shows two authored Attack icons centered over the first two border squares; the third slot remains empty |
| TC-ANL-001 through TC-ANL-007 | PASS | Analyze cost, rejection, one-use limit, deterministic enemy snapshot, node reset, unchanged Orbit, and unchanged player turn remain intact |
| Full battle, reward, resource, and Convergence regression | PASS | All pre-existing BattleSnapshotTests passed with no unexpected test error or exception |
| Windows Development Build | PASS | MainMenu and Battle compiled into a Windows x86_64 Development player with zero build errors |
| Human-controlled full standalone run | NOT RUN | Button-path automation, rendered Game view evidence, and player build passed; a full manual standalone playthrough is not claimed |

Reports: `Docs/TestResults/OrbitIconRuntime-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconRuntime-v0.4-GameView.png`.

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added verification artifacts: `Docs/TestResults/OrbitIconRuntime-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconRuntime-v0.4-GameView.png`.
- No new scene class, enum, listener, gameplay field, package, database entry, or `.meta` replacement was introduced. Existing `OrbitPanel`, legacy slot TMP references, icon sprite assignments, frame sprite, `OrbitRings`, battle controls, and Inspector file IDs remain connected.
- No manual Inspector setup is required. Reopening the Battle scene after Unity finishes importing the changed scene/script files is sufficient.

### Known Limitations and Next Milestone

The entrance is a single-panel authored motion and does not yet include particles, sound, controller rumble, or per-slot stagger. Those are presentation polish candidates after the run-state architecture is stable. Shadow Strike remains the only current skill with a dedicated symbol; unknown future skills use the generic skill sprite until their stable code-to-sprite entry is authored.

Continue with `RunStatController`, then seeded `RunManager` ownership of depth, resources, and modifiers. The complete selectable 8-10 node Standard Run map follows that state foundation.

## Orbit Action Icon Centering Follow-up 13 September 2026

### Corrected Behavior

- The authored Orbit frame is intentionally asymmetrical, so the visible centers of its three inner squares do not coincide exactly with the geometric center line of `OrbitPanel`.
- The three action `Image` overlays now use measured local centers from the frame artwork: slot 1 `(10, 101)`, slot 2 `(11, -2)`, and slot 3 `(11, -103)`.
- Attack, skill, and Guard symbols therefore sit in the visual center of their matching empty squares while retaining their 118 x 118 overlay size, upright rotation, aspect ratio, transparency, and render order above `OrbitFrameArtwork`.
- The preserved compatibility TMP objects remain at `(0, 112)`, `(0, 0)`, and `(0, -112)`. Their serialized references and file IDs were not repurposed or moved.
- This adjustment changes presentation coordinates only. Orbit recording, icon mapping, stage entrance animation, Convergence, Analyze, damage, mana, and reward behavior are unchanged.

### Verification Results

- Unity 6000.6.0f1 isolated Play Mode regression: **PASS**, 45 passed, 0 failed, 0 skipped in 29.09 seconds.
- `TC_ORB_002_AuthoredFrameAndActionIconsAreConfigured`: **PASS**. It now asserts the measured X/Y center of every authored icon in addition to overlay order, size, rotation, aspect, raycast, and reset state.
- Isolated 1600 x 900 Game View capture containing Attack, Shadow Strike, and Guard together: **PASS**. All three symbols render in the matching square centers.
- Windows x86_64 Development Build of MainMenu and Battle: **PASS**, Succeeded with 0 errors, 499 warnings, and output size 224,154,474 bytes.
- Human-controlled full standalone playthrough: **NOT RUN**. Automated button-path regression, visual capture, and player build passed.

Reports: `Docs/TestResults/OrbitIconCentering-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconCentering-v0.4-GameView.png`.

### Files and Inspector

- Modified: `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added verification artifacts: `Docs/TestResults/OrbitIconCentering-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconCentering-v0.4-GameView.png`.
- No new class, enum, runtime field, listener, scene object, package, database entry, or asset metadata was added. No manual Inspector setup is required.

## Orbit Runtime Center Enforcement Follow-up 13 September 2026

### Corrected Behavior

- A subsequent live Editor screenshot showed the Skill and Guard images still rendering at the former compatibility-label coordinates. Their actual rendered centers matched `(0, 112)` and `(0, 0)`, leaving them about 9-10 screen pixels left of the authored square centers and leaving the first slot about 10 screen pixels too high.
- `OrbitUIAnimator` now owns the three measured authored slot centers and reapplies them during `Awake` icon preparation and every slot transform reset: `(10, 101)`, `(11, -2)`, and `(11, -103)`.
- This makes the correction effective even when a previously open scene instance, stale serialized value, or runtime UI mutation supplies the old coordinates. Slot animation, icon repair, icon clearing, and action changes all settle back to the same authored centers.
- The correction is presentation-only. The existing scene coordinates, icon references, legacy TMP references, file IDs, action mapping, Orbit state, Convergence, Analyze, rewards, and battle progression remain intact.

### Verification Results

- Full isolated Play Mode regression: **PASS**, 45 passed, 0 failed, 0 skipped in 29.01 seconds.
- Runtime drift recovery: **PASS**. The test forces every action icon to `(0, 0)`, clears the icon presentation, and verifies that all three return to their measured authored centers.
- Live-screenshot scenario capture: **PASS**. A temporary capture helper forces the old slot-1 and slot-2 coordinates, then displays Shadow Strike in slot 1 and Guard in slot 2. The runtime animator corrects both before the 1600 x 900 Game View is rendered.
- Windows x86_64 Development Build: **PASS**, Succeeded with 0 errors, 500 warnings, and output size 224,154,547 bytes.
- Human-controlled standalone playthrough: **NOT RUN**.

Evidence: `Docs/TestResults/OrbitIconCentering-v0.4-PlayMode.xml` and `Docs/TestResults/OrbitIconRuntimeCentering-v0.4-GameView.png`.

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/OrbitUIAnimator.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- Added verification image: `Docs/TestResults/OrbitIconRuntimeCentering-v0.4-GameView.png`.
- No serialized field, scene object, class, enum, listener, package, database item, or `.meta` file was created or replaced. No manual Inspector assignment is required.

## RunStatController Milestone 13 September 2026

### Implemented Behavior

- Added `RunStatController` as the single calculator for locked Base Stats and temporary active-run modifiers. It is a plain C# object and does not create a scene object or Inspector dependency.
- The controller copies the validated Base Max HP, Base Max Mana, Base Basic Attack, and Base Guard reduction when a run is initialized. These values expose read-only properties and are never changed when rewards stack.
- Effective Max HP is Base Max HP plus all active `MaxHPFlat` modifiers. Vital Core still increases current HP by the exact reward snapshot value after the modifier is committed.
- Effective Basic Attack uses the existing additive reward percentage and the existing `Mathf.RoundToInt` behavior. The prototype Base Basic Attack remains 10.
- Effective Guard reduction uses the existing multiplicative strength formula and remains capped at 90% damage reduction.
- Effective Max Mana currently equals locked Base Max Mana because GDD v0.4 has no Max Mana reward in the implemented pool. Resource caps, Mana UI, and Analyze now read this effective value through the controller.
- Analyze cost is calculated by `RunStatController` from Effective Max Mana with `M - floor(M / 4)`, which is exactly `ceil(0.75 x M)` for non-negative integer Mana. Effective Max Mana 10 therefore costs 8.
- `BattleManager` remains the owner of current HP, current Mana, battle index, turn state, reward flow, and Orbit state. The milestone does not migrate those responsibilities or alter scene references.
- Existing private legacy reward totals and stack fields remain present and synchronized for compatibility with current tests and any existing reflection or Inspector tooling.

### Verification Results

Unity 6000.6.0f1 isolated Play Mode regression: **PASS**, 47 passed, 0 failed, 0 skipped in 28.96 seconds.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-RUN-001 | PASS | `RunModifierCollection` still aggregates, stacks, snapshots, and clears active-run modifiers |
| TC-RUN-002 | PASS | Locked Base Stats remain 100 HP, 10 Mana, 10 Basic Attack, and 50% Guard while Effective Stats consume all three reward types and reset to baseline |
| TC-RUN-003 | PASS | Repeated Guard Strength modifiers cannot raise effective damage reduction above 90% |
| Analyze numeric cases | PASS | Effective Max Mana is the Analyze source; all existing ceiling cases still pass, including Max Mana 10 costing 8 |
| Reward integration | PASS | Attack, Vital Core, and Guard rewards still apply once, stack, persist between battles, and clear on Restart Run |
| Resource integration | PASS | Guard restoration, Unstable Pulse, enemy-turn Mana recovery, restart clamp, and Mana UI use Effective Max Mana |
| Full battle and UX regression | PASS | Enemy snapshot, Analyze, Orbit, Convergence, reward presentation, victory, defeat, retry, stage transition, and icon runtime centering remain green |
| Windows Development Build | PASS | MainMenu and Battle built for Windows x86_64 with 0 errors, 500 warnings, and output size 224,156,838 bytes |
| Human-controlled standalone run | NOT RUN | Automated Play Mode integration and player build passed; a manual full-run playthrough is not claimed |

Report: `Docs/TestResults/RunStatController-v0.4-PlayMode.xml`.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RunStatController.cs`, its `.meta`, and `Docs/TestResults/RunStatController-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- `RunModifierCollection.cs`, `RewardManager.cs`, `RewardDefinition.cs`, the Battle scene, all serialized fields, all existing Inspector references, and existing `.meta` files were preserved without modification in this milestone.
- No manual Inspector setup is required.

### Known Limitation and Next Milestone

The current reward pool has no Max Mana modifier, so Effective Max Mana intentionally equals Base Max Mana. Skill damage remains defined by `SkillData`; Attack Surge continues to affect Basic Attack only, matching its current reward description.

The next milestone is a seeded `RunManager` that owns run depth, seed, current HP/Mana, and the active `RunModifierCollection` while keeping `BattleManager` as the battle executor. After that state foundation is verified, implement the selectable Standard Run node graph with approximately 8-10 nodes and a final boss.

## RunManager Milestone 14 September 2026

### Implemented Behavior

- Added `RunManager` as a plain C# active-run state owner. It does not create a singleton, `MonoBehaviour`, scene object, serialized field, or Inspector dependency.
- `RunManager` now owns the non-zero run seed, current depth, current HP, current Mana, total Embers, active/completed lifecycle flags, and the same `RunModifierCollection` consumed by `RunStatController`.
- Starting a new run clears all temporary modifiers, resets depth to 1 and Embers to 0, restores HP to locked Base Max HP, clamps configured starting Mana to locked Base Max Mana, and marks the run active and incomplete.
- Entering the next battle clamps current HP/Mana against Effective Max values without refilling them. Resource carryover therefore remains intact between nodes.
- Skill and Analyze Mana costs, Guard/Unstable Pulse/enemy-turn Mana restoration, enemy damage, and Vital Core current-HP restoration now mutate `RunManager` through explicit resource methods.
- Victory awards Embers through `RunManager`. Depth advances only after one committed reward selection; repeated clicks cannot advance the run or apply a modifier twice. Final victory marks the run completed, while defeat marks it inactive and incomplete until Restart Run.
- `BattleManager` remains the battle executor and continues to own enemy HP, turn state, EnemyActionPlan, Orbit, Convergence, UI flow, and reward presentation.
- The existing private fields `playerHP`, `playerMana`, `battleNumber`, `totalEmbers`, `runCompleted`, `rewardRunSeed`, and `runModifiers` remain present. They are synchronized compatibility mirrors/references so current reflection tests and existing integration contracts are preserved.
- Reward generation continues to derive a stable per-depth reward seed from the run seed. The next milestone will formalize a portable deterministic run-state snapshot and restore contract; no node-map generation or save system was added here.

### Verification Results

Unity 6000.6.0f1 isolated Play Mode regression: **PASS**, 49 passed, 0 failed, 0 skipped in 29.03 seconds.

| Test group | Result | Evidence checked |
| --- | --- | --- |
| TC-RUN-004 | PASS | RunManager owns the shared modifier collection, non-zero seed, depth, current HP/Mana, Embers, active/completed lifecycle, resource spending/restoration, damage/healing, and guarded depth advancement |
| TC-RUN-005 | PASS | Restart reuses one RunManager instance, clears modifiers and Embers, resets depth/resources/lifecycle, and synchronizes every compatibility field |
| Resource Carryover TC-RES-001 through TC-RES-006 | PASS | New-run initialization, per-node preservation, Vital Core current/max HP, Skill/Convergence Mana, node-entry clamping, and configured starting-Mana clamping remain correct |
| Reward single-commit regression | PASS | A rapid repeated card click applies one modifier and advances exactly one depth |
| Analyze TC-ANL-001 through TC-ANL-007 | PASS | Cost, once-per-node usage, no turn/Orbit mutation, snapshot identity, and per-node reset remain correct through RunManager resources |
| Full battle and UX regression | PASS | Attack, Skill, Guard, enemy turn, victory, defeat, retry, rewards, Orbit, Convergence, hidden intent, tooltip, artwork, icon centering, and stage entrance remain green |
| Windows Development Build | PASS | MainMenu and Battle built for Windows x86_64 with 0 errors, 499 warnings, output size 223,417,836 bytes, and build duration 1 minute 15.47 seconds |
| Human-controlled standalone run | NOT RUN | Automated Play Mode integration and player build passed; a manual full-run playthrough is not claimed |

Report: `Docs/TestResults/RunManager-v0.4-PlayMode.xml`.

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/RunManager.cs`, its `.meta`, and `Docs/TestResults/RunManager-v0.4-PlayMode.xml`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- `RunStatController.cs`, `RunModifierCollection.cs`, reward classes, EnemyActionPlan, Orbit classes, the Battle scene, all serialized fields, all existing Inspector references, and existing `.meta` files were preserved without modification in this milestone.
- No manual Inspector or scene setup is required.

### Known Limitation and Next Milestone

The run seed is generated at Restart Run and already stabilizes reward choices per depth, but active-run state is not yet represented by a serializable deterministic snapshot and cannot yet be reconstructed from an explicit seed. Standard Run node selection, deterministic node generation, checkpoint/save, and resume are also not part of this milestone.

The next roadmap item is **Seeded Run State**: define the deterministic, serializable state contract around the existing RunManager seed and lifecycle without implementing the Standard Run Node Map ahead of its own milestone.

## Enemy HP Early-Curve Balance Tuning 14 September 2026

### Implemented Behavior

- Tuned only the `max_hp` values in the tracked seed database for the current three sequential battles. The curve is now Battle 1 `40`, Battle 2 `50`, and Battle 3 `60`, down from `50`, `65`, and `85`.
- This is a data-only balance adjustment. No runtime multiplier, new scaling class, or BattleManager refactor was introduced, so `EnemyData`, `GameDatabase.GetEnemyByBattleIndex`, EnemyActionPlan, and existing Inspector contracts remain unchanged.
- Enemy attack damage remains `12`, `14`, and `17`; Embers rewards remain `10`, `18`, and `30`; and only Battle 3 remains the prototype boss. The HP reduction therefore does not change enemy intent, damage, reward, or victory rules.
- Battle UI continues to display `enemyHP/currentEnemy.MaxHP`, and enemy execution continues to use the already-prepared action snapshot. Both now read the same tuned seed data.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Seed database readback | PASS | Direct SQLite readback confirmed `max_hp` `40/50/60` and preserved damage, rewards, boss flags, codes, and names |
| Balance regression test source | PASS | Added `TC_BAL_001_EnemyMaxHPUsesGentleEarlyCurve` to `BattleSnapshotTests`; it asserts the exact curve and preserved combat metadata |
| Play Mode suite with new balance test | NOT RUN | The first isolated runner exited before tests because the global UPM cache returned `EPERM`; a retry with a temporary writable cache stalled in Unity Licensing and was stopped before producing a report |
| Previous RunManager regression | PASS (historical) | `Docs/TestResults/RunManager-v0.4-PlayMode.xml` records 49 passed, 0 failed, 0 skipped before this balance-only test was added |

### Files and Inspector

- Modified: `Assets/StreamingAssets/Database/game_seed.db`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No scene, serialized field, button listener, public API, `.meta` file, or Inspector reference was changed. No manual Inspector setup is required.

### Known Limitation and Next Milestone

The seed database still contains only the three prototype battles, so this tuning governs the current early sequence. The complete 8–10 node Standard Run and its future node-specific enemy data will need their own balance pass after the node map and seeded run-state contracts exist. At the time of this tuning the roadmap was **Seeded Run State**; that state contract is implemented in the follow-up below.

## Battle Settings Button Follow-up 14 September 2026

### Implemented Behavior

- Added `BattleSettingsUI` to the Battle Canvas. It creates one compact `68 x 68` Settings button at the lower-left anchor with a contrasting purple outline and a centered `42 x 42` procedural gear icon. The reduced footprint follows the compact control scale requested from the Anime Vanguards reference while retaining a comfortable click target.
- The gear is rendered by `GearIconGraphic`, a small custom UI mesh with eight teeth, so the symbol does not depend on a font glyph or an external texture and remains crisp at the CanvasScaler reference resolution.
- `GearIconGraphic` now explicitly requires and configures a `CanvasRenderer` (`cull = false`, `cullTransparentMesh = false`), writes `UIVertex` data directly, and forces its first geometry/material rebuild after sizing. This fixed the reported empty black Settings button on Unity 6; the visible gear was confirmed in the latest user screenshot.
- Clicking the gear opens a dark translucent Settings overlay with a purple-framed card, `SETTINGS` title, a `BATTLE PAUSED` status, and a `RESUME BATTLE` close action. The overlay blocks battle raycasts while visible.
- The Settings card now contains a simple two-column box layout: a category list (`Gameplay`, `Accessibility`, `Graphics`, and `Audio`) on the left, plus a category-specific option list on the right.
- Category buttons rebuild the right-hand option rows without changing BattleManager state. Toggle rows (`ON`/`OFF`) keep their value for the current Settings session; non-toggle rows show clearly marked prototype values such as `1x`, `100%`, and `ANALYZE ONLY`.
- Opening the panel stores the current `Time.timeScale` and pauses gameplay at zero. Closing the panel restores the exact previous scale and returns UI focus to the gear button. Cleanup also restores the scale if the scene is destroyed while the panel is open.
- The scene change adds only the `BattleSettingsUI` component to the existing Canvas. Existing BattleManager fields, button listeners, Orbit references, and Inspector assignments are untouched.
- Unity 6 TextMesh Pro compatibility is preserved by using `textWrappingMode = TextWrappingModes.NoWrap`; the obsolete `enableWordWrapping` property is no longer referenced.
- Runtime initialization is idempotent: `Awake`/`Start` repair an incomplete child hierarchy, and an `AfterSceneLoad` fallback adds the component only when a Battle scene has a `BattleManager` and no existing Settings component. The button is forced active and placed above other Canvas siblings so it remains visible.
- When the overlay opens, the Settings button is promoted above the dimming backdrop as well, keeping the gear readable and allowing the same control to close the panel; `RESUME BATTLE` remains available as the primary close action.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Scene reference audit | PASS | `BattleSettingsUI` points to the new script GUID on the existing Canvas; no BattleManager reference changed |
| Static UI contract | PASS | `TC_SET_001_SettingsButtonOpensAndRestoresBattlePause` asserts one gear child, a CanvasRenderer with opaque graphic colour, lower-left anchors, comfortable size, one-click open, pause, toggle close, time-scale restoration, and top-sibling layering while the overlay is open |
| Settings category/list contract | PASS (source) | `TC_SET_002_SettingsPanelShowsCategoryListAndOptionRows` asserts four category buttons, option labels, and category switching to the Accessibility list |
| TMP API compatibility scan | PASS | Repository search finds no `enableWordWrapping` usage; the Settings label uses the Unity 6 `textWrappingMode` API |
| Runtime visibility fallback | PASS (user verified) | The latest Play Mode screenshot shows the lower-left button, purple hover border, and the visible gear icon after the CanvasRenderer/mesh rebuild patch |
| Play Mode with Settings test | PASS (user verified) | The provided screenshot confirms the overlay opens with `SETTINGS`, `BATTLE PAUSED`, and `RESUME BATTLE`; the visible gear was confirmed before this category-layout change |
| Manual visual review of category layout | NOT RUN | The new two-column list and category switching require a fresh Play Mode screenshot after Unity recompiles the runtime script |

### Files and Inspector

- Added: `Assets/Project/Scripts/UI/BattleSettingsUI.cs` and its `.meta` file.
- Modified: `Assets/Project/Scenes/Battle.unity`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No manual Inspector assignment is required beyond the serialized Canvas component already stored in the scene. The script builds its child UI at runtime.

### Known Limitation and Next Milestone

The current panel is a safe entry point and pause shell. The category rows are a presentation layer: their values are not yet connected to audio mixers, display settings, input rebinding, animation speed, or BattleManager behavior. The latest resized button and the new category layout require a fresh live visual recheck after Unity recompiles the runtime script. The next gameplay milestone is **Standard Run Node Map** after the Seeded Run State follow-up.

## Seeded Run State Milestone 14 September 2026

### Implemented Behavior

- Added `SeededRunState`, a serializable state contract for the existing `RunManager` lifecycle. It records the schema version, ruleset version, run seed, depth, current HP, current Mana, total Embers, active/completed flags, and active-run modifiers.
- Added `SeededRunModifierState` as the serializable value object for each immutable runtime modifier. Modifier records are copied into the state and sorted by `SourceCode`, so dictionary iteration order cannot change a checkpoint identity.
- Added `RunManager.CaptureState()` and `RunManager.RestoreState(...)`. Capture never exposes the live modifier records; restore validates the schema, ruleset, seed, depth, lifecycle, resource bounds, modifier values, and duplicate source codes before replacing state.
- Effective Max HP and Max Mana remain derived by `RunStatController` and are supplied to restore as caps. The snapshot therefore does not create a second stat calculator or alter locked Base Stats.
- Added `RunModifierCollection.CreateStateSnapshot()` and `RestoreStateSnapshot(...)`. Restore builds a complete replacement collection first, so invalid serialized data cannot partially mutate the active run.
- Existing `RunManager` methods, `BattleManager` serialized fields, scene references, reward flow, resource carryover, and Inspector assignments remain intact. No scene setup is required for this state-only milestone.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Repository and duplicate-definition audit | PASS | Git root and required context were read; the new state classes have unique names and no existing class or enum was duplicated |
| State contract source audit | PASS | `SeededRunState` fields cover the current `RunManager` state and modifier collection without duplicating derived stats |
| Test assembly reference audit | PASS (source) | The first Console compile exposed direct production-type references in the test asmdef; `TC_RUN_006` and `TC_RUN_007` now use the existing reflection pattern and no longer require an `Assembly-CSharp` reference |
| Deterministic JSON round-trip test source | PASS (source) | `TC_RUN_006_SeededRunStateRoundTripsDeterministically` covers serialization, restore, modifier isolation, and different seed identity |
| Invalid restore guard test source | PASS (source) | `TC_RUN_007_SeededRunStateRejectsInvalidDataWithoutMutation` covers invalid seed rejection and no partial mutation |
| Unity compile after this milestone | NOT RUN | Unity compiler execution was unavailable in this session; no compile result is claimed for the new source |
| Play Mode `TC_RUN_006` and `TC_RUN_007` | NOT RUN | Unity Editor/Test Runner was not available after the code change |

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/SeededRunState.cs` and its `.meta` file.
- Modified: `Assets/Project/Scripts/Battle/RunManager.cs`, `Assets/Project/Scripts/Battle/RunModifierCollection.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No Inspector or scene assignment is required. The state contract is ready for a future `SaveService` or checkpoint repository to serialize, but no file I/O was added in this milestone.

### Known Limitation and Next Milestone

`SeededRunState` is currently an in-memory and serialization-ready contract. `BattleManager` does not yet save it to disk, generate a selectable node graph, or restore it through a Continue button. The next smallest implementation is **Standard Run Node Map**, backed by a deterministic node generator that consumes the run seed and keeps the final boss fixed at the end.

## Standard Run Node Map Milestone 14 September 2026

### Implemented Behavior

- Added `StandardRunNodeMap` as a plain serializable route model with public `RunSeed` and ordered `Nodes` fields. Each node stores a one-based `Depth`, `StandardRunNodeType`, stable `Code`, and `IsFinal` flag.
- Added `StandardRunNodeGenerator` with a small deterministic xorshift sequence. The same non-zero seed and node count always produce the same node order; the generator does not depend on Unity scene state or dictionary iteration order.
- The generator accepts 8, 9, or 10 nodes and defaults to the GDD target of 10. The first node is always `NormalBattle`, exactly one `EliteBattle` and one final `Boss` are generated, at least one `Rest` appears before the Boss, and no two Elite nodes can be adjacent. The route includes two or three `RandomEvent` nodes and at most one `Merchant`.
- `StandardRunNodeMap.Validate()` is the single structural guard for node count, depth ordering, unique codes, final-node flag, allowed node types, battle/event quotas, Elite/Boss counts, and the Rest-before-Boss rule.
- `GetNodeAtDepth()` and `TryGetNodeAtDepth()` expose the one-based depth contract used by `RunManager` while keeping invalid access explicit.
- No Battle scene, serialized field, Inspector reference, database schema, or `BattleManager` flow was changed. The current database still contains only three sequential battle encounters, so routing those encounters through all Standard Run node types is intentionally a later integration step.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Repository/context/duplicate audit | PASS | Git root, `Assets`, `Packages`, `ProjectSettings`, AGENTS, PROJECT_CONTEXT, and both GDD copies were read; no existing node type or map class was found before addition |
| Production source compile | PASS | `Add-Type` compiled `StandardRunNodeMap.cs` with no compiler errors |
| Deterministic route and rule sweep | PASS | 3,000 generated combinations across seeds `-500..500` (zero skipped) and node counts 8–10 validated successfully; a second 250-case random-seed sweep also passed |
| Invalid input/access guards | PASS | Zero seed, node counts 7/11, and out-of-range depth cases were rejected with the expected exceptions/results |
| Mutation guards | PASS | Valid lookup works, invalid lookup returns false/throws as designed, and corrupted depth or duplicate node codes are rejected by `Validate()` |
| Seed variation | PASS | Repeated seeds produced identical route keys; seeds 123 and 321 produced different route keys |
| Play Mode test source | PASS (source) | Added `TC_NODE_001` through `TC_NODE_005` for shape, determinism, 8–10 support, invalid inputs, and JSON round-trip using the existing reflection pattern |
| Unity compile and Play Mode execution | NOT RUN | The isolated runner reached Unity licensing initialization but timed out and produced no test XML; it was stopped before changing the live project |

### Files and Inspector

- Added: `Assets/Project/Scripts/Battle/StandardRunNodeMap.cs` and its `.meta` file.
- Modified: `Assets/Tests/PlayMode/BattleSnapshotTests.cs` and this context file.
- No scene or Inspector setup is required for this data-only milestone. Existing `BattleManager` serialized fields, button listeners, Orbit references, database files, and `.meta` GUIDs remain untouched.

### Known Limitation and Next Milestone

The map can now be generated, validated, queried, and serialized, but it is not yet displayed as a selectable UI map and does not choose enemy/event payloads. `RunManager.Depth` and the current prototype `BattleManager` still advance through the three database battle indexes. The next smallest step is to connect a generated map to `RunManager` and add a guarded node transition that routes Battle, Rest, Event, Elite, Merchant, and final Boss nodes without refilling carried resources.

## Standard Run Map Visual Direction Reference 14 September 2026

### Reference and Design Goal

- The user supplied an “Odyssey Route Atlas” screenshot as visual inspiration for the future SYNGRAVA Run Map: a modal route atlas with branching, connected nodes, a legend, a selected-node detail panel, and optional zoom controls.
- Treat the screenshot as hierarchy and usability inspiration only. Do not copy its artwork, typography, logos, exact layout, icons, or palette.
- The SYNGRAVA target is an original black-purple-white route map with a dark panel, a controlled purple border glow, branching connected routes, readable node icons, clear legend labels, a player position marker, and a detail panel for the selected node.
- Readability takes priority over decoration: route lines must remain distinct from ornaments, reachable choices must be obvious, and the final Boss route must remain easy to identify.
- The first implementation should be a functional map shell using existing palette values and procedural or placeholder UI. Authored art and polish can follow once deterministic map logic and node-transition tests are connected.
- Optional zoom controls and a right-side legend/detail panel are presentation goals taken from the reference, not a requirement to reproduce every control shown there.

### Planned Map Components

- Map modal/background panel and an original black-purple-white frame.
- Route connectors for inactive/locked, reachable/highlighted, travelled/completed, and current paths.
- Node icon set for Start/Current, Normal Battle, Elite Battle, Random Event, Rest, Merchant, and Boss.
- State overlays for locked, reachable, selected, completed, and current nodes. A tint, outline, or material can provide these states without duplicating every texture.
- Selection feedback with a purple outline/glow, hover/focus state, and selected-node detail panel.
- Header/title and close control; optional zoom-in, zoom-out, and reset controls.
- Legend icons and labels that match the node icon set.
- Small SYNGRAVA eye, orbit, and shard ornaments used sparingly around the frame and selection accents.
- Detail-panel backing, type-icon slot, title, description, reward, and requirement placeholders.
- Optional polish such as node pulse, route reveal, a travelling marker, and lightweight VFX. These must not block the functional map.
- Use TextMesh Pro for labels. Do not draw a separate font unless an original or properly licensed typeface is selected later.

### Asset Brief for Production

Minimum authored or procedural assets:

1. One transparent 9-slice or tileable map-frame texture.
2. One dark map-panel/background texture, or a procedural panel with the approved SYNGRAVA palette.
3. Node icons for Normal Battle, Elite Battle, Random Event, Rest, Merchant, Boss, and Start/Current.
4. State overlays for locked, reachable, selected, completed, and current; these may be UI materials, shader effects, outlines, or procedural shapes.
5. Route/connector textures or vector/procedural dashed lines for inactive, reachable, and travelled states.
6. A matching legend icon set.
7. One close/exit icon and optional zoom icons.
8. Small SYNGRAVA eye/orbit/shard ornaments for frame corners and selection accents.
9. Optional detail-panel ornament/background.

Recommended production format:

- Keep layered PSD/ORA/SVG sources where possible and export transparent PNG/SVG at approximately 2x the intended reference size.
- Use square canvases and consistent padding for node icons so their hit areas and visual centers line up.
- Make the outer frame a 9-slice asset when it must stretch across resolutions.
- Keep black dominant, purple as the accent/glow, and white for readable content and active highlights.
- Avoid copying the reference game's skull art, logo, exact typography, or red palette.

### Acceptance Direction for the Future RunMap

- The same `RunSeed` produces the same map and route connectivity.
- Only reachable nodes are selectable; travelled and locked nodes reject clicks.
- Player position updates after a node is resolved.
- The selected node's type, title, description, and detail panel match its data.
- The final Boss is clearly represented and remains the last node.
- Reopening the map does not regenerate a different route.
- The map remains inside safe Canvas bounds at the project's responsive 16:9 design resolution.

## Run Node Lifecycle Integration Milestone 19 September 2026

### Implemented Behavior

- `RunManager` now creates and owns one `StandardRunNodeMap` when a run starts. The existing `StartNewRun(...)` API remains intact and creates the approved default ten-node route; `StartNewRunWithNodeCount(...)` supports the generator's validated 8–10 node range without adding an ambiguous overload.
- `NodeMap`, `CurrentNode`, and `HasNextNode` expose the active deterministic route to the future RunMap UI and encounter router. The route seed always matches `RunManager.RunSeed`.
- `TryResolveCurrentNode(expectedType, expectedNodeCode)` is the guarded node transaction. It accepts only the current node's exact type and stable code, rejects stale callbacks and wrong node handlers without changing depth, advances one node at most, and completes the run when the final Boss node is resolved.
- Repeating a callback from a node that was already resolved cannot resolve the next node because its stable code no longer matches. Repeating the final Boss callback is also rejected after the run is complete.
- Resolving a node changes only run position and lifecycle. Current HP, current Mana, Embers, and the modifier collection are not refilled, reset, or rewritten by node movement.
- The legacy `TryAdvanceDepth()` API remains available for existing callers and now uses the same guarded current-node path. It refuses to advance beyond the final node.
- `SeededRunState` schema is now version 2 and records `NodeCount`. Capture stores the map size; restore validates the 8–10 node range and depth bounds, regenerates the exact route from `RunSeed + NodeCount`, then restores runtime state.
- Route generation and checkpoint validation occur before mutable runtime state is replaced. A corrupt node count or out-of-range depth therefore cannot partially replace resources or modifiers.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Production model compile | PASS | PowerShell `Add-Type` compiled `RewardDefinition`, `RunModifierCollection`, `StandardRunNodeMap`, `SeededRunState`, and `RunManager` together with no compiler errors |
| Deterministic node lifecycle sweep | PASS | 1,500 combinations covering seeds 1–500 and node counts 8, 9, and 10 completed through the guarded API; every run ended on its Boss and preserved HP/Mana |
| Invalid and duplicate transaction guards | PASS | Every sweep case rejected a wrong node type, foreign code, repeated previous-node callback, and repeated completed-Boss callback without extra advancement |
| Snapshot restore and corruption sweep | PASS | 250 additional seeds restored the same route, depth, HP, and Mana; an out-of-range checkpoint depth was rejected without mutating the target run |
| Play Mode test source compile | PASS | Added `TC_NODE_006_RunManagerOwnsAndGuardsNodeProgression` and `TC_NODE_007_SeededStateRestoresTheSameNodeMap`; the Unity-generated test project compiled with 0 errors and 0 warnings using temporary MSBuild output |
| Unity isolated Play Mode execution | NOT RUN | Two isolated Unity 6000.6.0f1 attempts stopped before compilation because the Unity Licensing Client repeatedly lost its IPC connection; no test XML was produced and no Play Mode PASS is claimed |

### Files and Inspector

- Modified: `Assets/Project/Scripts/Battle/RunManager.cs`, `Assets/Project/Scripts/Battle/SeededRunState.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No Battle scene object, serialized BattleManager field, button listener, database row, art asset, public battle API, or `.meta` GUID was changed.
- No Inspector or Hierarchy assignment is required for this lifecycle milestone.

### Known Limitation and Next Milestone

The deterministic map is now part of the run lifecycle, but the current `BattleManager` prototype still presents its original three sequential database battles and does not dispatch `NormalBattle`, `EliteBattle`, `RandomEvent`, `Rest`, `Merchant`, or `Boss` from `CurrentNode`. The map also remains an ordered route model rather than a visible branching selection screen.

The next smallest milestone is a **RunMap encounter router and functional placeholder UI**: display the generated route, mark current/reachable/completed states, permit only valid node selection, and dispatch each selected node type to its appropriate scene or placeholder handler. That integration must preserve resource carryover and use `TryResolveCurrentNode` as the only commit point.

## RunMap Encounter Router and Functional UI Milestone 19 September 2026

### Implemented Behavior

- Added `StandardRunMapUI`, a runtime UI component attached to the existing Battle Canvas without adding a serialized `BattleManager` field or changing the Battle scene. It displays the complete 8–10 node route, route connectors, progress, a legend, selected-node details, and one contextual enter button.
- The current route is shown after `RestartRun()` and after every non-final battle reward. Battle command and Orbit panels are hidden while the map is open and restored only after a valid battle node is entered.
- Completed, current/reachable, and locked nodes have separate presentation states. Only the exact current-depth node is interactable. Locked and completed buttons reject selection.
- The UI callback passes the node's stable code to `BattleManager`. The router validates that the map is open and that the code still matches `RunManager.CurrentNode`; foreign, stale, repeated, and post-close callbacks cannot advance depth or restart the encounter.
- `NormalBattle`, `EliteBattle`, and `Boss` dispatch to the existing Battle scene flow. Until dedicated encounter pools exist, Normal nodes alternate the two non-boss database prototypes, Elite uses the second prototype, and the final Boss uses database battle index 3. This mapping does not alter database rows, enemy stats, or serialized scene references.
- A non-final battle opens exactly one reward selection. Committing the reward resolves that exact battle node through `RunManager.TryResolveCurrentNode(...)` and returns to the Run Map. Repeated reward clicks remain guarded.
- `RandomEvent`, `Rest`, and `Merchant` use explicit functional placeholders. Selecting one resolves the exact current node and returns to the next map position without changing current HP, current Mana, Embers, Orbit, or run modifiers. No unapproved healing, price, event outcome, or balance value was invented.
- `RUN COMPLETE` is now reached only after the route's final Boss node is defeated and committed. Exhausting the three current database encounter rows no longer ends the run early.
- A new run resets Analyze usage, Guard state, enemy-plan snapshot, and Orbit before the map opens. Entering each battle still creates a fresh deterministic `EnemyActionPlan`, resets Analyze for that node, and preserves carried HP/Mana.

### Route Node Map Visual Production Milestone

The functional shell deliberately uses procedural rectangles, TextMesh Pro labels, outlines, and the approved black-purple-white palette. Authored artwork is a separate visual-production milestone so art replacement cannot destabilize routing or gameplay.

Planned visual deliverables:

1. One original transparent 9-slice outer frame and one optional inner/detail frame in the SYNGRAVA rough eye/orbit/shard language.
2. One dark map backing texture with subtle, low-contrast Singularity marks that does not compete with route lines.
3. Centered, consistently padded icons for Start/Current, Normal Battle, Elite Battle, Random Event, Rest, Merchant, and Boss.
4. Reusable state overlays or UI materials for locked, reachable, selected, current, and completed nodes.
5. Connector assets or materials for inactive, reachable, and travelled routes, always rendered behind node buttons.
6. A player-position marker, matching legend icons, selection marker, close icon, and optional zoom-in/zoom-out/reset icons.
7. Optional frame-corner eyes, shards, and orbit scratches used sparingly so text and route readability remain dominant.
8. Layered PSD/ORA/SVG source files plus transparent PNG/SVG exports at approximately 2x display size. Node icons must use identical square canvases and visual-center padding; stretchable frames must be prepared for Unity 9-slicing.

Visual acceptance requirements:

- Black remains dominant, purple communicates route state and selection, and white is reserved for active/readable information.
- Every node state must remain distinguishable without pointer hover and at the project's 16:9 reference resolution.
- Node icons must remain centered inside their hit areas, connectors must not overlap labels, the final Boss must be immediately recognizable, and the entire route/detail panel must stay inside Canvas safe bounds.
- The supplied route-atlas screenshot remains hierarchy and usability inspiration only. Its skull art, logo, typography, exact layout, palette, and game-specific symbols must not be copied.
- Authored visual replacement must preserve the existing routing API, node codes, selection guards, and Play Mode tests.

### Verification Results

| Check | Result | Evidence |
| --- | --- | --- |
| Production source compile | PASS | Unity-generated `Assembly-CSharp.csproj` built with 0 errors; the only warning is the pre-existing obsolete `FindFirstObjectByType` call elsewhere in `BattleManager` |
| Play Mode test source compile | PASS | `Syngrava.Battle.PlayModeTests.csproj` built with 0 errors and 0 warnings using temporary MSBuild output |
| Initial isolated Unity run | FAIL, FIXED | 63/64 passed; the failure correctly exposed that Analyze/Orbit were reset on battle entry instead of at the new-run boundary |
| Final isolated Unity Play Mode run | PASS | 64/64 tests passed, 0 failed, 0 skipped in Unity 6000.6.0f1; execution used an isolated project and product name so the live runtime database was not touched |
| `TC_MAP_001` | PASS | New run opens the map, creates one button per route node, exposes matching details, and leaves exactly the current node reachable |
| `TC_MAP_002` | PASS | Valid selection starts one battle; foreign, stale, and repeated post-close callbacks do not advance or recreate the plan |
| `TC_MAP_003` | PASS | Rest/Event/Merchant placeholder resolution advances once and preserves HP, Mana, Embers, and modifier ownership |
| Full-run regression | PASS | Defeat/restart resets run state, all route nodes can be traversed, rewards return to the map, and completion occurs only after the final Boss |

### Files and Inspector

- Added: `Assets/Project/Scripts/UI/StandardRunMapUI.cs` and `Assets/Project/Scripts/UI/StandardRunMapUI.cs.meta`.
- Modified: `Assets/Project/Scripts/Battle/BattleManager.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context file.
- No scene, database, package, existing `.meta` GUID, serialized `BattleManager` field, public API, or Inspector reference was removed or renamed. The existing Battle Canvas receives the map component at runtime, so no manual scene assignment is required.

### Known Limitations and Next Milestone

- The current data model is a deterministic ordered route displayed as a two-row path. True branching and multiple reachable choices require a later connectivity model and must not be faked only in UI.
- Rest, Random Event, Merchant, and dedicated Elite content are placeholders with no effect. Their full systems remain separate milestones.
- Encounter routing reuses the three current database prototypes. Dedicated seeded encounter pools, enemy variants, and node-specific rewards are not part of this milestone.
- The current Run Map uses procedural placeholder visuals and has no authored route-map textures, icons, zoom controls, travel animation, or branching animation yet. The visual-production brief above is the approved preparation target.
- Following the established feature order, the next technical milestone is **Checkpoint and Save Active Run**, using `SeededRunState` schema 2 and the current node code/depth as the restore boundary. Authored Run Map visuals can proceed in parallel after the required original assets are available and approved.

## Fractured Fate Web Visual Selection and Asset Pack 19 September 2026

### Approved Direction

- The user selected visual concept 2, **Fractured Fate Web**, as the official target for the Standard Run Map.
- The approved full-design reference is stored at `Assets/Project/Art/UI/RunMap/FracturedFate/Reference/RunMap_FracturedFate_ApprovedReference.png`.
- The target composition is an asymmetric branching web inside torn black-paper planes, with rough white ripped edges, deep-purple fractures and scratches, separate node plaques, a prominent current-position eye, and a right-side node-detail frame.
- Palette priority is black first, deep purple second, and off-white third. White is reserved for readable symbols and active route information; purple communicates selection, reachability, and Singularity energy.
- The reference is an original SYNGRAVA design direction. It does not authorize copying the supplied inspiration game's skull art, exact layout, logo, typography, or other game-specific artwork.

### Created Asset Package

All authored PNG assets live under `Assets/Project/Art/UI/RunMap/FracturedFate/` and have stable Unity `.meta` files.

- `Backgrounds/RunMap_FracturedBackdrop.png`: low-contrast matte black/purple map backing.
- `Frames/RunMap_FracturedOuterFrame.png`: transparent full-map torn-paper frame overlay.
- `Frames/RunMap_FracturedDetailFrame.png`: transparent portrait frame for selected-node details and its action footer.
- `Nodes/RunMap_NodePlaque.png`: reusable empty node plaque for separately layered icons.
- `Nodes/RunMap_IconBattle.png`: Normal Battle symbol.
- `Nodes/RunMap_IconElite.png`: Elite Battle symbol.
- `Nodes/RunMap_IconEvent.png`: Random Event symbol.
- `Nodes/RunMap_IconRest.png`: Rest symbol.
- `Nodes/RunMap_IconMerchant.png`: Merchant symbol.
- `Nodes/RunMap_IconBoss.png`: Boss symbol.
- `Nodes/RunMap_IconCurrent.png`: current-position and Start marker.
- `Markers/RunMap_StateRing.png`: reusable selection/reachability/current halo.
- `Connectors/RunMap_ConnectorStroke.png`: reusable transparent route stroke that can be rotated, stretched, and tinted for route states.
- `Ornaments/RunMap_FracturedOrnaments.png`: optional torn-paper, shard, scratch, and eye accents.

Transparent assets were checked for a real alpha channel. Their importer mode is explicitly set to one sprite per file, preventing Unity's automatic alpha-island slicing from turning one icon into several separate sprites.

### Visual State Plan

- Locked, completed, reachable, selected, and current states should reuse the same plaque, icon, connector, and state-ring sprites with controlled tint, alpha, scale, outline, and pulse. Do not duplicate a full texture set for every state.
- Connectors render behind nodes. Inactive routes use low-alpha gray-violet, reachable routes use clear violet, travelled routes use off-white with subdued violet, and the current selection may use a restrained pulse.
- Node icons remain separate from plaques so their visual centers can be tuned independently and icon colors can change without recoloring the frame.
- The detail frame should contain runtime TextMesh Pro labels and buttons; no text is baked into the raster asset.

### Remaining Work Before the Approved Design Is Playable

1. Replace the functional placeholder rectangles in `StandardRunMapUI` with the authored backdrop, frame, plaque, icon, marker, and connector sprites.
2. Add a true deterministic branching-connectivity model. The current run model is still an ordered route, so the visual must not pretend that multiple branches are selectable until data and validation support them.
3. Implement responsive node layout, connector rotation/stretching, safe-area handling, and selected-node detail binding.
4. Implement locked, reachable, completed, selected, and current visual states plus hover/focus feedback.
5. Add the map opening/reveal animation, route reveal, node pulse, and travelling marker after selection rules are stable.
6. Add optional close and zoom controls only if navigation testing shows that the full 8-10-node graph needs them.
7. Run Unity import, compile, Play Mode route-selection regression, and multi-resolution visual checks after integration. Asset creation alone does not count as a playable Run Map implementation.

### Scope Boundary

This milestone creates and registers the visual asset package only. It does not alter `BattleManager`, `StandardRunMapUI`, the Battle scene, route generation, node resolution, resource carryover, or Inspector references. Visual integration and branching behavior are the next separate implementation milestone.

## Fractured Fate Web Runtime Integration - 20 September 2026

### Implementation Status

The authored **Fractured Fate Web** package is now connected to the Standard Run Map that opens at run start and after each completed non-final battle reward. This section supersedes the earlier asset-only scope boundary while preserving its art direction and branching limitation.

Implemented behavior:

- `StandardRunMapUI` loads one `StandardRunMapVisualAssets` catalog from `Resources/RunMap/FracturedFate/StandardRunMapVisualAssets` at runtime.
- The Battle scene and `BattleManager` require no new serialized fields or Inspector references. The existing runtime attachment to the Battle Canvas remains unchanged.
- The map uses the authored fractured backdrop, torn outer frame, detail frame, reusable node plaque, six node-type icons, current-position eye, state ring, connector stroke, and ornament overlay.
- The former two-row placeholder grid is replaced with a deterministic asymmetric 8-10-node route composition. It still represents the single ordered route owned by `StandardRunNodeMap`; it does not display fake selectable branches.
- Completed, current, and locked nodes use distinct tint and alpha. Only the current depth remains interactable.
- The current node uses a pulsing state ring and eye marker driven by `Time.unscaledTime`, so the animation continues correctly while battle time is paused by the modal map.
- The map card enters with a short unscaled fade, lift, and scale animation. The full-screen overlay continues to block battle input.
- The selected node's authored icon, type, depth, route code, description, and context-specific action label are shown in the right detail panel.
- The icon legend is now sprite-based and matches the same runtime catalog used by route nodes.
- If the visual catalog is absent, the existing procedural colors and text glyphs remain a defensive fallback. The committed catalog is complete and verified by Play Mode test.

### Files Added

- `Assets/Project/Scripts/UI/StandardRunMapVisualAssets.cs`: typed ScriptableObject catalog and node-type icon resolver.
- `Assets/Project/Resources/RunMap/FracturedFate/StandardRunMapVisualAssets.asset`: runtime sprite references for the approved art package.
- Unity `.meta` files for the new script, Resources folders, and catalog asset.

### Files Updated

- `Assets/Project/Scripts/UI/StandardRunMapUI.cs`: authored visuals, asymmetric layout, visual states, detail icon, legend, current-node pulse, and opening animation.
- `Assets/Tests/PlayMode/BattleSnapshotTests.cs`: `TC_MAP_004_AuthoredFracturedFateVisualsAreBound` verifies the catalog, frame, plaques, type icons, connectors, and selected detail icon.
- The generated Run Map texture importer `.meta` files remain configured as one full-rect sprite per PNG so each complete authored element is addressable as a single Unity Sprite.

No Battle scene serialized reference, existing BattleManager API, reward flow, run-state schema, or user-authored `BattleVisual.cs`/TMP font change was replaced by this integration.

### Verification

Compile verification:

- `Assembly-CSharp.csproj`: PASS, 0 errors. One existing `FindFirstObjectByType` deprecation warning remains in `BattleManager.cs` and is unrelated to this milestone.
- `Syngrava.Battle.PlayModeTests.csproj`: PASS, 0 warnings, 0 errors.

Official isolated Unity Play Mode run through `Tools/Run-BattleSnapshotTests.ps1`:

- Total: **65 PASS, 0 FAIL, 0 SKIPPED**.
- `TC_MAP_001_NewRunShowsOnlyCurrentRouteNodeAsReachable`: PASS.
- `TC_MAP_002_SelectionDispatchesOnceAndRejectsStaleCallback`: PASS.
- `TC_MAP_003_PlaceholderPreservesResourcesAndAdvancesOnce`: PASS.
- `TC_MAP_004_AuthoredFracturedFateVisualsAreBound`: PASS.
- No `NullReferenceException`, duplicate callback, compiler error, or run-resource regression was reported.

### Known Limitation and Next Milestone

- The visual route is intentionally a single deterministic path because `StandardRunNodeMap` still stores one ordered node sequence. True multi-branch connectivity, multiple reachable choices, and branch-aware checkpoint validation remain planned; the UI must not imply those choices before the model supports them.
- Zoom, pan, close, travel-line drawing animation, and marker travel between nodes remain optional polish after branch rules are stable.
- The next feature-order milestone remains **Checkpoint and Save Active Run**, using `SeededRunState` schema 2 and the current node code/depth as the restore boundary. A later branching-map milestone should extend the saved state with selected branch identity before enabling multiple choices.

## Fractured Fate Map Composition Correction - 20 September 2026

### User-approved correction

The live screenshot exposed an art-composition regression in the earlier integration: the complete ornament atlas was drawn over the route, the full outer frame covered the header, and the locked-node tint was multiplied by Button's disabled tint. Passing sprite-reference tests did not establish visual fidelity.

The user requested removal of that atlas background and placement of its separate elements according to the approved Fractured Fate Web concept. The approved reference remains `Art/UI/RunMap/FracturedFate/Reference/RunMap_FracturedFate_ApprovedReference.png` under `Assets/Project/`.

### Current composition

- The complete ornament sheet and full-frame composite are no longer rendered over the map. The original PNGs and GUIDs are retained as source art.
- `StandardRunMapUI` makes individually named Sprite slices from the source atlas, without changing its pixels or requiring readable texture import. These are independent runtime Image objects: header tear, left paper spine, footer tear, upper/lower corner crystals, left eye accent, and detail-panel edge accents. Sprite slices are released with the UI component.
- Source slice regions use normalized top-left coordinates in `BuildSeparatedFrame`. Position, size and rotation are controlled per element; never restore the full sheet as a background Image.
- All perimeter decorations live under `RunMapOuterFrame`, below the title, route panel and detail content. They ignore raycasts.
- The map card has a solid near-black backing. Only a quiet central crop of the existing paper texture appears inside the route area; the large purple veil has been removed.
- The route remains center-left, with a narrow decorative spine to its left, title above, icon legend below, and an independently readable right detail panel. The eye position marker sits beside the current plaque instead of on top of its action-type symbol.
- Route-stroke transparent padding is cropped through a separate Sprite view. Locked nodes retain readable symbols and receive their state tint only once.
- A separate fit transform scales the whole card to the Canvas while preserving the opening animation and its internal proportions.

### Verification

The correction is verified together with the branching-map milestone below: 70/70 isolated Unity Play Mode cases passed, including a real first-battle victory, automatic reward selection, and rendered return to the map at three resolutions. The user also accepted the separated-element composition before requesting real branch choices. The earlier 65-test report remains historical.

### Scope and setup

No manual Inspector assignment is required. Changes are limited to map presentation, its regression tests and documentation. Battle/reward routing, run state, scene references, source PNG pixels, and unrelated user edits are preserved.

At the time of the composition correction the route was still ordered. The branching milestone below supersedes that limitation. A matching node-detail environment illustration remains outstanding.

## Deterministic Branching Run Map - 20 September 2026

### Approved contract and implemented behavior

The user approved the corrected Fractured Fate composition, then requested a connected web with several actual choices. Standard Run still visits **8-10 stages**, with one chosen encounter per stage. The complete graph contains **16, 19 or 22 nodes** for 8, 9 or 10 stages respectively; unchosen alternatives do not count as completed encounters.

- `StandardRunNodeMap.Nodes` and `Count` preserve the canonical depth-based contract. `Alternatives`, `AllNodes`, `Lane` and each node's `NextNodeCodes` expose the complete directed graph. The partial class in `StandardRunBranchGraph.cs` extends the existing model; it is not a duplicate map type.
- The opening node is a single Normal Battle. Choice rows have three lanes. Compatible paths can cross or merge; all paths converge through the shared Elite and Rest stages and end at one Boss.
- Lane content and connectivity are deterministic from the run seed and stage count. Alternate lanes shuffle the same encounter pool. Cross-lane connections are permitted only when encounter-prefix counts agree, so every complete path retains the GDD composition: 3-5 Normal Battles, 2-3 Events, exactly one Elite and Boss, at least one Rest before Boss, and at most one Merchant.
- Validation checks unique identities and row lanes, forward edges, no missing/duplicate destinations, no dead ends, reachability and encounter quotas for every possible path. Lines are created from actual graph edges only.
- `RunManager.TryEnterNode` accepts only a connected choice at the current depth, then locks its identity until resolution. Repeated entry, stale codes and foreign/unconnected nodes are rejected. `TryResolveCurrentNode` commits the chosen code once, records history, then advances one stage.
- `CurrentNode` retains a deterministic reachable default for existing depth-based APIs. The map UI does not commit that default when multiple choices exist: it displays `CHOOSE YOUR PATH` and requires an explicit click followed by confirmation.
- `BattleManager` dispatches the chosen node through the existing battle or placeholder handler. Non-final victories still open one reward selection and return to the map after the reward is committed. Carried HP/Mana and temporary modifiers remain owned by the same active run.
- The UI displays three lanes from bottom to top. Its masked viewport supports mouse-wheel scrolling and dragging, and opening the map frames the previous position and current choices. Decorations, title, legend and details remain fixed outside the scroll content.
- Connected choices have equal purple pulse feedback. The eye marks the last visited node; cleared, available, selected, skipped and future nodes have distinct text/state treatment. Available connections are solid violet, travelled connections off-white, and future connections dashed. Only connected choices are interactable.
- The existing three prototype enemy records are reused. Alternate Normal Battle lanes can select the other non-boss prototype; no enemy stat, reward value, database row or encounter content was added.

### Branch-aware snapshot

`SeededRunState` now uses schema **3**, adding copied `VisitedNodeCodes`, `SelectedNodeCode` and `NodeEntered` fields. Restore regenerates the same graph and validates the exact path and pending/entered choice before mutating resources or modifiers. A corrupt path cannot partially restore a run.

Schema 2 is explicitly rejected rather than silently guessing a branch. No disk-based Active Run save or Continue flow existed in this slice, so this is snapshot support, not a completed save/resume feature. Future checkpoint work must persist schema 3 and the branch identity.

### Verification and retained evidence

Two complete isolated Unity 6000.6.0f1 runs were performed: the first passed 69/69; the final run after clearer dashed paths, equal choice pulses and added corruption coverage passed **70/70**, with **0 failures, 0 skipped**, duration **43.153 seconds**, Unity exit **0**. All seven changed production/test source files match the passing isolated copy byte-for-byte.

| Check | Result | Evidence |
| --- | --- | --- |
| Unity import and runtime/test compilation | PASS | The final isolated Play Mode suite compiled and executed successfully |
| TC_BRANCH_001 | PASS | 300 seed/stage combinations generate identical graphs for identical inputs, validate every route's quotas, reject double entry/resolution, preserve HP/Mana and finish at Boss |
| TC_BRANCH_002 | PASS | Entered and post-resolution snapshots restore the chosen branch and next choices; foreign history/selection is rejected without partial mutation; cloned history is independent |
| TC_BRANCH_003 | PASS | Three connected choices appear; preview does not advance the run; locked-node callbacks cannot change selection; confirmation dispatches the chosen encounter |
| TC_BRANCH_004 | PASS | Backward, foreign, duplicate and dead-end edges, invalid path quotas and null alternatives are rejected |
| TC_MAP_001 through TC_MAP_004 | PASS | Initial state, routing/callback guards, placeholder carryover and complete visual bindings remain correct |
| TC_MAP_005 | PASS | Actual first-battle victory and reward return to a three-choice map; sliced decorations stay behind content; card, viewport and details fit at 1920x1080, 1280x720 and 1280x1024 |
| TC_ANL_001 through TC_ANL_007 and existing battle/reward/run tests | PASS | All earlier regression cases are included in the 70 passing tests |
| Offscreen Unity render inspection | PASS | All three captured layouts were visually inspected; paths and three choice highlights are visible and detail content stays inside its frame |
| Human-controlled standalone playthrough / new Windows build | NOT RUN | This milestone verifies Editor Play Mode and rendered captures, not a standalone release |

Retained report: `Docs/TestResults/BranchingRunMap-v0.4-PlayMode.xml`.
Retained Unity renders: `Docs/TestResults/BranchingRunMap-1920x1080.png`, `BranchingRunMap-1280x720.png`, and `BranchingRunMap-1280x1024.png`.

### Files and setup

- Added: `Assets/Project/Scripts/Battle/StandardRunBranchGraph.cs` and its `.meta`, the retained XML report and three render captures.
- Updated: `StandardRunNodeMap.cs`, `RunManager.cs`, `SeededRunState.cs`, `BattleManager.cs`, `StandardRunMapUI.cs`, `Assets/Tests/PlayMode/BattleSnapshotTests.cs`, and this context.
- No new Inspector assignments or scene edits are needed. The existing Battle Canvas receives the runtime map as before.
- The user's staged `BattleVisual.cs` edit, TMP fallback-font changes and `ProjectSettings.asset` define-symbol change were preserved. Existing map art, GUIDs and catalog references were retained; no commit or reset was performed.

### Remaining work

Checkpoint and Save Active Run is the next smallest technical milestone, now using schema 3. Rest, Random Event and Merchant still use the previously documented no-effect placeholders. Dedicated Elite/Boss behavior, richer encounter pools and node-detail environment illustrations remain separate work. The map now supports real branch choices; it does not claim those unimplemented encounter systems are complete.

## Checkpoint and Save Active Run Milestone - 20 September 2026

### Implemented contract

`ActiveRunCheckpointStore.cs` now persists one committed Standard Run checkpoint under `Application.persistentDataPath/Saves/syngrava-active-run-v0.4.json`. The persisted state is the existing `SeededRunState` schema 3; no second run-state DTO or competing branch model was introduced.

- The file uses a versioned envelope containing the serialized schema 3 state, an ISO UTC save timestamp, and a SHA-256 integrity value over the exact state JSON.
- Writes go to a `.tmp` file first, flush through the file stream, then atomically replace the previous checkpoint. A platform fallback keeps the previous file available for rollback if the replacement move fails.
- The store refuses inactive, completed, or `NodeEntered` states. This deliberately prevents a mid-battle, half-reward or half-node transaction from replacing the last safe map-boundary checkpoint.
- Loading verifies envelope version and SHA-256 before deserializing the run state. Restore delegates to `RunManager.RestoreState`, retaining its schema, ruleset, resource, modifier and branch-path validation. Rejected/corrupt data cannot partially mutate the target run.
- A new run writes an initial safe checkpoint. Non-final battle checkpoints are written only after reward application and `TryResolveCurrentNode` succeed. Placeholder nodes save after resolution. Defeat and final boss completion remove the active checkpoint.
- Closing during an encounter therefore returns to the last committed map boundary when Continue is wired later; unfinished battle damage, Embers and reward changes are not committed.

### Runtime integration

`BattleManager` owns one runtime `ActiveRunCheckpointStore` without adding serialized fields or Inspector dependencies. Existing scene references, button callbacks, map routing, Orbit, reward, user-edited `BattleVisual.cs`, TMP fallback font and ProjectSettings changes remain intact.

The persistence layer intentionally includes `LoadActiveRun` and `RestoreActiveRun` so the next milestone can wire Continue without changing the file contract. No Main Menu or Continue button was added in this milestone.

### Verification

Three complete isolated Unity 6000.6.0f1 Play Mode runs passed after implementation and the final stale-save guard review. Each run executed **74/74 tests**, with **0 failures and 0 skipped**. The final run duration was **47.747288 seconds** and Unity exited with code **0**.

| Check | Result | Evidence |
| --- | --- | --- |
| Production and test assembly compilation | PASS | Unity imported the new store and executed the complete suite |
| TC_SAVE_001 | PASS | A committed branched run saves, loads and restores to byte-equivalent state JSON |
| TC_SAVE_002 | PASS | Entered, defeated and otherwise unsafe boundaries are rejected without overwriting the last safe file |
| TC_SAVE_003 | PASS | Tampered/corrupt checkpoint data fails integrity validation and cannot mutate the target RunManager |
| TC_SAVE_004 | PASS | Restart creates a checkpoint, reward resolution advances and replaces it, and defeat deletes it |
| Existing battle, Analyze, reward, resource, map and branch regressions | PASS | All preceding tests remain included in the 74 passing cases |
| Restart stale-save guard | PASS | Restart deletes the previous checkpoint after the new run is constructed, then writes and verifies the new run state |

Retained report: `Docs/TestResults/ActiveRunCheckpoint-v0.4-PlayMode.xml`.

### Files and setup

- Added: `Assets/Project/Scripts/Battle/ActiveRunCheckpointStore.cs`, its `.meta`, and the retained Play Mode XML report.
- Updated: `BattleManager.cs`, `BattleSnapshotTests.cs`, this context, and the GDD implementation-status appendix.
- No Inspector assignment, scene edit, package change, database migration or manual folder setup is required.

### Remaining work and next milestone

This milestone stores and validates the active run but does not expose a player-facing Continue flow. The next milestone is **Continue and Corrupt Save Recovery**: inspect checkpoint availability on startup/Main Menu, restore it with current effective stat limits, present a clear incompatible/corrupt-save message, and provide a deliberate discard/start-new decision. A backup-generation recovery policy remains separate from the current integrity rejection. Rest, Random Event, Merchant, dedicated Elite/Boss content and node-detail illustrations remain unchanged placeholders or future content.

## Continue and Recovery UI Plan - 20 September 2026

### Placement decision

`MainMenu.unity` currently contains only a camera and is not the playable entry used by the Battle Play Mode suite. The first implementation therefore uses a runtime startup modal on the existing Battle Canvas. No manual scene edit or Inspector assignment is required. A future authored Main Menu can reuse the same checkpoint inspection contract and visual states.

### UI state matrix

| Checkpoint state | Title | Primary action | Secondary action | Runtime behavior |
| --- | --- | --- | --- | --- |
| No checkpoint | No modal | Start automatically | None | Preserve the current first-run flow |
| Valid primary | CONTINUE RUN | Continue Run | Start New Run | Restore the exact schema 3 branch state or deliberately discard it |
| Invalid primary, valid backup | RECOVERY AVAILABLE | Recover Run | Discard and New Run | Restore the last verified backup boundary, then promote it to the active checkpoint |
| Invalid primary and backup | SAVE DATA DAMAGED | Recheck Save | Discard and New Run | Never mutate RunManager until a valid state exists or the player deliberately discards the files |

### Layout and interaction contract

- Full-screen charcoal backdrop blocks raycasts and prevents battle input before a decision.
- A centered black-purple card uses a high-contrast violet outline, white title, subdued explanatory copy and a small state label.
- The summary presents Seed, current Node/total Nodes, HP, Mana, Embers and last visited route code. It contains no technical JSON or filesystem detail.
- Primary action is visually brighter than the destructive/new-run action. Destructive wording is explicit; recovery is never performed silently.
- Opening uses a short unscaled fade and scale animation so Time.timeScale cannot strand the modal.
- The modal scales through the existing Battle Canvas and remains inside the target 1920x1080, 1280x720 and 1280x1024 layouts.

### Storage and safety plan

- Every successful replacement retains the previous verified checkpoint as a `.bak` recovery boundary.
- Inspection validates primary and backup independently through the same envelope, SHA-256, schema, ruleset, resource, modifier and branch checks used by RunManager restore.
- Preview inspection never mutates the live run. Only a confirmed Continue or Recover action replaces live state.
- Successful recovery promotes the backup to the primary checkpoint. Discard removes primary, backup, temporary and rollback files before starting a clean run.

## Continue / Recovery and Route Readability Revision - 20 September 2026

User direction: finish and test Continue/Recovery first, then replace its generic box styling with the approved SYNGRAVA Fractured Fate language. Use Higgsfield for an original visual asset if available. Keep black dominant, purple secondary, and white for legibility. Roughness belongs on decorative edges; navigation lines and text must stay orderly.

Plan before presentation edits:

1. Verify real scene reload, modifiers above base HP, backup recovery, damaged files, discard, and repeated callbacks. Recovery writes a verified replacement before changing the live run.
2. Save panel: restrained torn-paper frame, eye emblem, left-aligned title, concise journey summary, clearly dominant Continue/Recover action and quieter New Journey action. Hide underlying battle placeholders behind an opaque charcoal backdrop. Keep text and hit areas as separate runtime UI elements.
3. Map: align the three lane centers and stage rows, trim connectors before node plaques, use continuous thin paths instead of many fragmented dash sprites, and highlight immediate choices. Preserve the deterministic graph and saved branch identities; do not change the generator merely to make lines look simpler.
4. Remove internal route IDs and enum-style names from player-facing details. Show Battle / Elite / Unknown Encounter / Rest / Merchant / Boss with short useful descriptions. Keep unfinished encounter types honest about their current lack of rewards or healing.
5. Inspect actual Unity renders at 1920x1080, 1280x720 and 1280x1024. Re-run the full regression after presentation changes. Record results separately from pending visual approval.

Primary files: ActiveRunCheckpointStore.cs, BattleManager.cs, ActiveRunRecoveryUI.cs, StandardRunMapUI.cs, BattleSnapshotTests.cs. Existing package and project-setting edits are user changes and remain outside this task.

### Implementation evidence - 21 September 2026 (visual acceptance pending)

Continue/Recovery and the readability revision are implemented. This supersedes the earlier proposed save summary: the current UI shows Stage, HP, Mana and Embers, with no raw seed or route code. Continue restores to the map; Recovery promotes a validated backup before replacing live state. A locked recovery write leaves both the live state and valid backup intact. Stale button callbacks cannot rewind an already resumed battle.

The first functional suite passed 78/78. The full suite after presentation edits and extra safety cases passed 81/81, with zero failures or skips. Actual Unity renders were captured at 1920x1080, 1280x720 and 1280x1024. Evidence and implementation details are in `Docs/TestResults/2026-09-21-ContinueRecovery/REPORT.md` and `final-results.xml`. Testing used an isolated product identity to protect the player's saves.

Higgsfield generation was attempted with the supplied reference but returned `Requires basic plan or higher`. No new generated art is claimed. The implementation uses the existing approved Fractured Fate assets as separate decorative elements. Lines are continuous and nodes align to lanes; connections may still cross where the actual saved graph branches.

No Inspector or Hierarchy setup is required. Standalone Windows build validation, a manual full run, and owner approval of the revised visuals remain outstanding. Do not mark final visual design accepted in the GDD until that review. Rest, event and merchant gameplay interactions remain their existing future milestones.

Double-check: a separate rerun of all seven Continue/Recovery tests passed 7/7 with no failures. Hash comparisons confirmed the isolated test project used exactly the same five changed source/test files as the working project. See `recheck-results.xml` beside the full report.

## Progressive route reveal and Rest choices - 21 September 2026

User direction: hide later nodes and their connections until the current node has been completed, remove the overlapping decorative spine text, and implement a choice of 50% HP or 50% Mana recovery at Rest.

UI plan and rules:

- A new run displays only its starting node. After resolving it (including the battle reward step), show the connected second-stage choices. Third-stage nodes remain hidden until the second stage resolves, and so on.
- Keep completed nodes as a route history. Hide unchosen branches after advancing. Only travelled links and links to the immediate choices are visible. Increase vertical stage spacing from 132 to 190 UI units.
- The deterministic graph is generated from the run seed once. Reveal does not reroll it: Continue/Recovery reconstructs exactly the same next choices. This is staged discovery of the seeded route, not a new random roll every time the map opens.
- Remove `FATE / FRACTURES / ALL PATHS` from the decorative spine.
- Selecting Rest shows two separate actions in the existing detail panel: RESTORE 50% HP and RESTORE 50% MANA. The interpretation of the requested HP option is healing by half of effective maximum HP, not a heal to full health. Fractions round up; both resources are capped at their effective maximum.
- Choose exactly one recovery. The choice resolves the Rest node, saves the resulting map boundary, then reveals the next choices. A repeated callback for the resolved Rest cannot heal again. Recovery is available even if the chosen resource is already full; it then restores zero without exceeding the cap.
- No new sprite, package, scene or Inspector assignment is needed. Existing event/merchant placeholder behavior remains separate.

Implementation files: StandardRunMapUI.cs, BattleManager.cs, BattleSnapshotTests.cs.

Verification complete: two full isolated Unity PlayMode runs passed 85/85 each. After hiding the tiny node captions as well as the spine text, all nine focused map/Rest cases passed again (9/9). The tested script hashes match the working project. Wide and narrow Unity renders were inspected, including the Rest choices at 1280x1024; text overflow and panel bounds checks passed. No manual Inspector setup is required.

Evidence: `Docs/TestResults/2026-09-21-ProgressiveRest/REPORT.md`, the full/focused XML reports and the map/Rest screenshots beside it. This implements the requested reveal and Rest behavior; standalone Windows build validation and owner visual acceptance are not claimed. Random Event and Merchant remain future gameplay work.

## Random Event implementation - 21 September 2026

Milestone selection follows the GDD's newer ordered integration plan (Checkpoint, Continue/Recovery, Rest and Random Event, Merchant, dedicated Elite/Boss, then Relic/Anomaly/Status). This is the next part after the completed Rest request; it does not claim the earlier 31-item backlog is entirely complete.

Design before UI: reuse the existing map detail panel with two vertically separated action buttons. Display a short encounter story and the exact cost/outcome of both options. Disable unaffordable options. Keep this as a resource-choice milestone; no new art, combat status system or database migration.

`RunEventService` owns an immutable three-event catalog, stable seed-plus-node-code selection, affordability and the one-time resource transaction. `StandardRunMapUI` presents the selected event; `BattleManager` saves only after the chosen event is resolved, then returns to the progressively revealed map. UI buttons share the existing alternative-action layout with Rest; Rest public accessors remain compatible with existing callers/tests.

Provisional content/balance:

| Event | First choice | Second choice |
| --- | --- | --- |
| The Ember Altar | Pay 10 HP, gain 15 Embers; must retain at least 1 HP | Gain 3 Embers without HP cost |
| The Silent Well | Pay 3 Mana, heal 25% of effective maximum HP | Restore 2 Mana |
| The Ashen Cache | Gain 10 Embers | Heal 15% of effective maximum HP |

Healing rounds upward and all recovery is capped. Costs are checked before entry or mutation. Invalid choice indices, foreign/nonreachable nodes and repeats after resolution are rejected. The existing checkpoint records resulting HP, Mana, Embers and visited node history. Reopening an uncommitted event produces the same encounter; Continue restores the committed outcome without granting it twice. No separate event-choice journal has been added.

Validation completed with two final runs on unchanged code, as requested: **89/89 passed** (50.89 seconds) and **89/89 passed** (50.10 seconds), zero failures/skips. Dedicated tests cover 60 seeds, all three encounters, unaffordable costs, invalid/repeated choices, actual UI selection, disk save/Continue, and wide/narrow renders for each event. Legacy battle-only navigation fixtures now bypass optional noncombat effects explicitly; event/Rest effects are tested independently through their real UI and transaction paths.

Evidence: `Docs/TestResults/2026-09-21-RandomEvent/REPORT.md`, first-results.xml, second-results.xml, and six Unity screenshots. All six captures were visually inspected; text overflow and detail-panel bounds checks passed at 1920x1080 and 1280x1024. Source hashes match the working project. No Inspector assignment is required. Standalone Windows build, manual complete-run verification and final content/balance acceptance are not claimed. The next integration milestone is Merchant and resource economy.

## Merchant and unified map layout - 21 September 2026

User direction: continue the next milestone, keep token use focused, run exactly two final Unity test passes after programming, and use Canva/Figma for future in-game UI/UX/HUD design. Preserve SYNGRAVA's black, violet, white and torn-paper/crystal art direction. Do not claim final art acceptance before the owner reviews the result.

Design plan and reference: https://www.figma.com/design/cFuAmGd1jgxacnD53crytC (frame 2:2). The Figma layout uses a continuous map/sidebar body with zero gutter, a thin shared seam, and aligned top/bottom edges. It is a layout specification, not a replacement for the existing authored sprites. Canva searches returned no SYNGRAVA designs or available brand templates; no Canva-generated asset is claimed. Figma uses Roboto as a documented layout substitute because Liberation Sans/Arial were unavailable in the font query; Unity retains its current Liberation Sans. The Figma preview was inspected. A subsequent programmatic Figma check was not executed because automatic approval review encountered a usage limit.

Implementation:

- Expand the route panel by 57 UI units into the former empty gutter while preserving node/viewport coordinates. The right edge now meets the detail panel's left edge exactly. Reuse continuous top and bottom paper ornaments instead of separate disconnected detail strips.
- Merchant offers one supply per visit: Healing Balm costs 12 Embers and restores 50% of effective maximum HP; Focus Tonic costs 8 Embers and restores 50% of effective maximum Mana. These prices are provisional balance values. Recovery rounds upward and is capped. Full resources and unaffordable purchases are disabled.
- Leave Merchant always permits progression without spending or healing. Buying or leaving resolves the node once and saves the resulting map boundary. Revisiting an already resolved transaction is rejected. Continue restores the resulting resources and visited path; no new save schema is necessary.
- RunMerchantService owns the merchant rules; RunManager owns spending; StandardRunMapUI displays balance, prices and three choices; BattleManager dispatches and checkpoints. No Inspector setup or new sprite import is required.
- This is the first resource-trading milestone, not a full shop inventory, relic vendor, reroll system or permanent economy. Dedicated Elite/Boss encounters remain the next integration milestone.

Verification results are recorded in `Docs/TestResults/2026-09-21-Merchant/REPORT.md` after the two requested final runs. Standalone Windows build and manual complete-run validation are separate outstanding checks.

Actual verification: first run 93/93 passed; second run 92/93 passed, with one legacy branch-navigation fixture failure when a randomly chosen Merchant could not be purchased by a fresh run. All dedicated Merchant cases and layout captures passed in both runs. The fixture now chooses Leave Merchant for branch-navigation coverage; this test-only correction has not been rerun because the requested two-run limit was reached. Production code remained unchanged across/after both runs and matched the isolated source hashes. Do not describe this milestone as two green full regressions. The corrected fixture needs verification before that claim is made. Merchant wide/narrow and Ashen Cache seam screenshots were visually inspected and are included with the report.

Follow-up verification (supersedes the pending status above): the user explicitly authorized one additional full Unity test run. It passed **93/93**, zero failures/skips, in **52.20 seconds**, including the corrected TC_BRANCH_003 navigation fixture. All four changed production files and the test file match the successful isolated run by hash. No further code edits were needed. Evidence: `Docs/TestResults/2026-09-21-Merchant/third-results.xml` and the updated REPORT.md. Merchant implementation and automated regression verification are complete; manual full-run, standalone build and owner visual/balance acceptance remain separate.

## Elite / Boss combat and sliding introductions - 21 September 2026

User authorized necessary testing without the earlier two-run limit. References specify a dramatic warning banner and a sideways character-introduction composition; retain SYNGRAVA's existing black/violet/white language, with crimson Boss accents. Do not import the reference game's characters or branding.

Design plan: a central, slightly tilted banner enters from the left for Elite and from the right for Boss. Timing: 0.45 seconds entry, 1.15 seconds hold, 0.5 seconds exit toward the opposite side. The current enemy name and existing Elite/Boss emblem identify the encounter. Separate accent rails reinforce movement. Battle input is blocked during the cut-in. Pause freezes it; disabling Motion Effects shows the announcement without sliding. No tween package or external animation plugin is required. Figma motion/layout specification is in https://www.figma.com/design/cFuAmGd1jgxacnD53crytC (Elite frame 3:2, Boss frame 3:8).

Implemented combat rules (provisional balance):

- Elite alternates a base-damage attack with Ashen Crush at 135% base damage, rounded up.
- Boss repeats Void Strike, Gathering the Abyss (zero damage), then Abyssal Rupture. Rupture deals 150% base damage before awakening. The charge log warns that a heavy strike follows.
- When the living Boss is at or below half HP after an enemy turn, a one-time PHASE II cut-in announces awakening. Future Void Strikes deal 125% base damage and Rupture 175%. Boss HP remains database-controlled; no extra HP multiplier was introduced.
- Prepared action objects remain immutable. Crossing the phase threshold during a player action does not secretly replace the action already prepared/revealed by Analyze. Elite/Boss prepare their next action only after the current enemy turn resolves. Normal-battle plan behavior is preserved. Analyze remains once per node and hidden intent remains the default.
- Guard reduces damage normally. Charge causes no damage or hit/lunge effect. Existing reward, final victory and checkpoint rules are preserved. Restart/EndBattle cancel cut-ins; generation checks prevent a cancelled animation from dismissing a newer one.
- EncounterBannerUI is attached at runtime to the battle Canvas. Existing art is loaded from StandardRunMapVisualAssets; no Inspector assignment is required. This does not provide new Boss character art, arena art, music or sound effects.

Verification and actual Unity renders are recorded in `Docs/TestResults/2026-09-21-EliteBoss/REPORT.md`. The first implementation run passed 95/95. The final run includes additional slide-direction, reduced-motion and cancellation coverage after visual refinement. Final counts will be recorded in the report. Owner visual/balance acceptance and a standalone Windows build remain separate from automated verification.

Final evidence: **96/96 PlayMode tests passed**, zero failures/skips, in 60.17 seconds. The new tests cover Elite/Boss cycles, charge, phase II, input locking, pause, opposite slide directions, reduced motion and cancellation. Final Unity renders at 1920x1080 and 1280x1024 were inspected; text overflow and label containment checks passed. Five changed source/test hashes match the isolated tested copies. See final-results.xml and boss-banner images in the report directory. Automated implementation verification is complete; bespoke Boss portrait/arena/audio and owner acceptance remain separate production work.

## Relic System: empat jenis dan node pilihan — 22 September 2026

Arahan terbaru pengguna: buat empat jenis relic terlebih dahulu, tambahkan Node Relic sebagai alternatif terhadap node lain, dan gunakan kembali pemilihan tiga kartu upgrade. Relic harus memiliki kegunaan berbeda, tanpa versi sejenis yang langsung lebih kuat. Tidak menambahkan relic ke reward Elite pada tahap ini.

Alur yang diterapkan: selesaikan stage sebelumnya → baris stage 3 terbuka → pilih cabang Relic Shrine yang terhubung atau encounter lainnya → shrine menampilkan tiga relic berbeda dari katalog empat → pilih satu → efek tersimpan untuk run aktif → kembali ke peta. Satu run memiliki satu shrine. Player tetap menempuh 8–10 stage; shrine menggantikan satu encounter awal, bukan menambah panjang run. Elite, Rest menjelang akhir, dan Boss tetap ada.

| Relic | Efek | Batas agar tidak bertambah tanpa kendali |
| --- | --- | --- |
| Hollow Crown | Guard memulihkan tambahan 1 Mana; total pemulihan langsung menjadi 3 | Tetap dibatasi maksimum Mana efektif; tidak menaikkan stat dasar |
| Ashen Fang | Basic Attack menambahkan 1 Burn selama 2 giliran enemy; damage 1 per stack setelah enemy bertindak | Maksimum 3 stack; aplikasi ulang menyegarkan durasi; hilang pada battle berikutnya |
| Ember Locket | Memulihkan 6 HP saat memasuki battle | Tidak melewati maksimum HP; tidak langsung heal ketika kartu dipilih |
| Fractured Lens | Skill pertama yang berhasil pada setiap battle memberi tambahan 6 damage | Sekali per battle; percobaan saat Mana kurang tidak menghabiskan efek |

Hollow Crown dan Ashen Fang berasal dari contoh GDD. Ember Locket dan Fractured Lens merupakan tambahan konten sementara untuk memenuhi permintaan empat jenis tanpa menunggu sistem Speed, Curse, atau Critical. Angka di atas adalah balance awal: flat, terbatas, dan bergantung pada kebutuhan build. Pengujian otomatis membuktikan aturan efek berjalan, bukan membuktikan seluruh relic memiliki win rate identik. Playtest ekonomi dan kombinasi jangka panjang tetap diperlukan.

Keempat relic bersifat unik, tanpa tier dan tanpa duplicate stacking. Pemilihan tiga kartu mengikuti seed secara deterministik sehingga membuka ulang shrine yang belum dikomit tidak mengubah penawaran. Pilihan hanya dapat dikomit sekali. Save/Continue menyimpan identitas relic dan jalur yang sudah ditempuh. Nilai modifier relic yang rusak/ditumpuk tidak sah ditolak sebelum state aktif diubah. Efek hanya berlaku ketika run aktif; Restart menghapus build lama.

Node Relic ditambahkan sebagai lane keempat pada stage 3. Koneksinya mengikuti node lane kiri di stage yang sama; node asli tetap ada. Artinya akses shrine mengikuti koneksi predecessor, bukan selalu tersedia dari semua jalur stage 2. Posisi empat node memakai -360, -120, 120, 360 dalam viewport 1000 unit. Progressive reveal dan pemisahan map/detail tetap dipertahankan. Quota komposisi canonical route tetap berlaku; jalur shrine secara nyata mengganti satu encounter dengan relic. Validator memeriksa kompatibilitas cabang berdasarkan encounter yang digantikan, bukan menghitung shrine sebagai battle tambahan.

Kompatibilitas: RouteVersion 1 adalah graph baru. Checkpoint lama yang belum memiliki field tersebut diperlakukan sebagai versi 0 dan memulihkan graph lama tanpa shrine tambahan. Enum lama dipertahankan urutannya. Schema dan hash envelope checkpoint tidak diganti. Untuk melihat shrine baru, mulai run baru; Continue save lama sengaja mempertahankan rute lamanya.

UI memakai panel tiga kartu, animasi masuk, hover dan penguncian klik lama. Label RELIC dan UNIQUE / ACTIVE RUN menggantikan tier/stack. Warna teks dibuat terang agar terbaca di panel gelap. Desain susunan dicatat di Figma yang sama, frame 5:2: https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=5-2 . Font Figma Roboto adalah substitusi; Unity tetap Liberation Sans. Belum ada asset Canva baru. Ikon shrine memakai eye marker yang sudah ada, dan kartu memakai glyph sementara; ilustrasi empat relic yang khusus masih pekerjaan art berikutnya.

Bukti dan detail pengujian: `Docs/TestResults/2026-09-22-Relic/REPORT.md`. Dua regresi awal lulus 102/102 (62.56 dan 62.41 detik). Pemeriksaan gambar menemukan teks kartu terlalu gelap meskipun pemeriksaan overflow lulus; kontras diperbaiki dan tes keterbacaan ditambahkan. Hasil verifikasi sesudah perbaikan dicatat dalam laporan tersebut. Tidak ada assignment Inspector baru. Build Windows dan manual full-run belum diklaim.

Pekerjaan berikutnya: perluasan Anomaly dan Status Effect System sesuai GDD; Burn pada Ashen Fang saat ini merupakan efek battle khusus, belum framework status umum untuk Poison, Curse, durasi lintas sumber, dan sebagainya. Multiple shrine, katalog relic tambahan, layar koleksi relic, art khusus, serta tuning berdasarkan playtest belum selesai. GDD DOCX tidak ditulis ulang pada iterasi ini; dua relic sementara di atas perlu disahkan sebagai keputusan konten saat pengguna meninjau hasilnya.

Hasil final sesudah perbaikan kontras: **102/102 PlayMode lulus**, tanpa kegagalan/skip, **63.94 detik**. Gambar 1920×1080 dan 1280×1024 diperiksa secara visual; teks terang terbaca dan deskripsi Ashen Fang tetap berada dalam kartu. Dua belas hash file source/test cocok dengan salinan yang diuji. Implementasi fungsional empat relic, node opsional, pemilihan kartu dan save/restore selesai diverifikasi otomatis. Bukti final: `verified-results.xml`, `source-hashes.csv`, dan dua screenshot pada folder laporan Relic di atas.

## Status Effect framework dan Anomaly tahap pertama — 23 September 2026

Arahan pengguna: kerjakan Status Effect/Debuff/Anomaly dengan urutan Status Effect lebih dahulu, pertahankan konsekuensi Anomaly yang benar-benar berfungsi, lalu uji perubahan sebelum menyatakan selesai.

### Status Effect battle-scoped

`BattleStatusController` sekarang menjadi pemilik tunggal status sementara selama battle. Status tidak masuk `RunModifierCollection` dan tidak disimpan pada checkpoint map-boundary. `StartBattle` membersihkannya sehingga efek battle lama tidak terbawa ke encounter berikutnya.

| Status | Aturan yang diterapkan |
| --- | --- |
| Burn | Damage akhir giliran sebesar jumlah stack; durasi awal 2 giliran; maksimum 3 stack; aplikasi baru menambah stack dan menyegarkan durasi |
| Weakness | Outgoing damage unit yang terkena dikalikan 0,75 dan dibulatkan sekali pada hasil akhir |
| Curse | Incoming damage unit yang terkena dikalikan 1,20 dan dibulatkan sekali pada hasil akhir |

Status mendukung target Player dan Enemy, snapshot read-only untuk HUD, refresh, tick tepat sekali, expiry dan reset. Ashen Fang tidak lagi memiliki perhitungan Burn sendiri: relic tersebut memanggil controller yang sama. Field `relicBurnStacks` dan `relicBurnTurns` dipertahankan sebagai mirror kompatibilitas tes/reflection lama; controller tetap source of truth.

HUD HP menampilkan badge teks ringkas hanya ketika status aktif, misalnya `BURN x2 · 1 TURN`. Player damage melewati urutan outgoing Weakness lalu incoming Curse. Enemy plan execution memakai jalur yang sama sebelum Guard. Intent Rewrite Weaken tetap merupakan mekanik terpisah dan tidak disamakan dengan Weakness.

### Anomaly run-scoped tahap pertama

`RunAnomalyCatalog` menambahkan dua Anomaly yang seluruh benefit dan consequence-nya dapat dijalankan oleh battle model saat ini:

| Anomaly | Benefit | Consequence |
| --- | --- | --- |
| Collapsed Star | Semua Skill damage +30% | Semua Skill cost +1 Mana |
| Black Sun | Convergence pertama setiap battle selesai setelah dua aksi | Effective Max HP -15, minimum tetap 1 |

Black Sun memakai aturan eksplisit untuk sequence dua aksi: slot pertama dianggap wildcard. Resolver mencoba prefix Attack, Guard, lalu Skill untuk menemukan Named Pattern yang valid; jika tidak ada, hasilnya Unstable Pulse. Bonus dua-slot dikonsumsi tepat sekali per battle. Collapsed Star menghitung biaya aktual sebelum transaksi Mana, memperbarui label tombol, dan menerapkan bonus sebelum Convergence menghitung bonus dari action damage.

Anomaly bersifat unik, tidak dapat stack, disimpan sebagai modifier Active Run, divalidasi saat restore, dibersihkan saat New Run, dan Black Sun ikut dihitung oleh effective-stat/checkpoint cap. Setelah kemenangan Elite, reward flow menawarkan Anomaly yang belum dimiliki. Karena baru dua Anomaly yang benar-benar didukung, UI menampilkan dua kartu yang dipusatkan dan menyembunyikan kartu ketiga. Jika seluruh Anomaly tahap ini sudah dimiliki, Elite kembali ke reward biasa.

Frozen Orbit dan Inverted Pulse belum ditawarkan. Frozen Orbit membutuhkan initiative/Speed yang berfungsi, sedangkan consequence Inverted Pulse membutuhkan Critical yang benar-benar aktif. Menampilkannya sekarang akan menghasilkan drawback palsu. Target akhir GDD tetap empat Anomaly dan tiga pilihan berbeda; tahap berikutnya harus membangun fondasi Speed/Critical atau menyepakati pengganti yang tidak bertentangan dengan GDD.

### Verifikasi aktual dan batas klaim

- Unity 6000.6 mengimpor source dan berhasil reload assembly tanpa compiler error. Percobaan sandbox awal tertahan pada IPC Unity Licensing Client; final run dijalankan di luar sandbox pada salinan project terisolasi sehingga save/database pengguna tetap tidak disentuh.
- `dotnet restore` dan build solution dengan dua source baru dimasukkan sementara ke `.csproj` generated menghasilkan **0 error** dan 4 warning API obsolete yang sudah ada pada UI lama. File `.csproj` dipulihkan setelah build.
- Dua smoke-runtime terisolasi terhadap source yang sama lulus dua kali. Pemeriksaan meliputi Burn cap/refresh/tick/expiry, Weakness 25%, Curse 20%, dua pilihan Anomaly deterministik dan unik, larangan duplicate stack, serta Black Sun Effective Max HP 85 dari base 100.
- Full Unity Play Mode final: **107/107 PASS**, 0 failure, 0 skipped, durasi **67.6479246 detik**. Run pertama mencapai 106/107; satu helper tes lama menunggu tombol ketiga yang sengaja disembunyikan pada pilihan dua Anomaly. Helper diperbaiki agar hanya menunggu kartu aktif, lalu seluruh suite lulus tanpa perubahan production code tambahan.
- Coverage baru `TC_STATUS_001`–`002` dan `TC_ANO_001`–`003` memeriksa damage pipeline, Collapsed Star benefit+cost, Black Sun dua-slot sekali per battle, persistence dan reset. `TC_RELIC_006` sekarang membuktikan Ashen Fang memakai controller status bersama.
- Tidak ada scene, Inspector assignment, package, SQLite schema atau asset visual baru yang diperlukan.

Laporan dan hasil final: `Docs/TestResults/2026-09-23-StatusAnomaly/REPORT.md` serta `final-results.xml`.

Pekerjaan berikutnya pada catatan tahap pertama di atas telah diselesaikan pada tahap penyelesaian berikut. Gunakan bagian terbaru di bawah sebagai status yang berlaku.

## Penyelesaian Status Effect / Debuff / Anomaly — 23 September 2026

Arahan pengguna: lanjutkan pekerjaan Status Effect, Debuff dan Anomaly yang tersisa. Implementasi harus mempertahankan konsekuensi Anomaly yang benar-benar berfungsi, memakai alur kode yang rapi, diuji, dan baru dinyatakan selesai setelah hasil final diperiksa.

### Empat Anomaly aktif

Katalog sekarang berisi empat Anomaly sesuai ruang lingkup GDD. Pilihan Elite menampilkan maksimum tiga Anomaly unik yang belum dimiliki. Seed yang sama dan state yang sama menghasilkan pilihan yang sama.

| Anomaly | Benefit | Consequence | Aturan runtime |
| --- | --- | --- | --- |
| Collapsed Star | Skill damage +30% | Skill Mana cost +1 | Cost aktual divalidasi sebelum Mana dikurangi; bonus damage masuk ke jalur Convergence lama |
| Black Sun | Convergence pertama setiap battle selesai setelah 2 aksi | Effective Max HP -15 | Hanya sekali per battle; sequence dua aksi mencoba Named Pattern yang valid lalu fallback ke Unstable Pulse |
| Frozen Orbit | Guard menahan 2 serangan enemy yang menghasilkan damage | Player Speed -2 | Base Speed 10 menjadi 8; enemy baseline 9 membuka setiap round selama Anomaly aktif |
| Inverted Pulse | Basic Attack yang berhasil memulihkan 2 HP | Basic Attack tidak dapat Critical | Heal dibatasi Effective Max HP; Skill masih dapat Critical |

Semua Anomaly memakai `RunModifierCollection`, bersifat unik, tidak dapat stack dengan dirinya sendiri, ikut disimpan oleh payload checkpoint yang sudah ada, dipulihkan oleh Continue, dan dibersihkan oleh New Run. Dua nilai enum baru ditambahkan pada akhir enum agar nilai lama tidak bergeser. Tidak diperlukan kenaikan checkpoint schema.

### Fondasi Speed, initiative dan Critical

`BattleTurnRules` menjadi tempat aturan deterministik lintas battle:

- Player Base Speed 10 dan minimum Effective Speed 1.
- Enemy baseline Speed sementara 9 karena model `EnemyData`/SQLite saat ini belum memiliki kolom Speed.
- Jika Speed sama, Player bertindak lebih dahulu.
- Battle start dan setiap round baru membandingkan Speed. Jika enemy membuka round, enemy tidak mendapat giliran kedua setelah aksi Player pada round yang sama.
- Base Critical chance 5%; Critical multiplier 1,5 dan dibulatkan sekali setelah pipeline damage/status.
- Critical roll berasal dari run seed, route depth dan strike index pada battle. Continue atau replay terhadap sequence aksi yang sama menghasilkan hasil yang sama.
- Inverted Pulse hanya memblokir Critical Basic Attack. Skill tetap mengikuti Critical normal.

`playerStrikeIndex`, state initiative, dan hit Guard Frozen Orbit direset pada batas battle. Guard Frozen Orbit hanya mengonsumsi hit saat action benar-benar menghasilkan damage; action charge Boss dengan zero damage tidak menghabiskan proteksi.

Fondasi ini cukup untuk model duel satu Player melawan satu Enemy sekarang. Jika game nanti mendukung beberapa enemy/ally, baseline Speed enemy harus dipindah ke data content dan sistem ini perlu diubah menjadi initiative queue yang mengurutkan seluruh unit.

### Sumber nyata Weakness dan Curse

Status runtime sebelumnya sudah benar tetapi Weakness/Curse belum bisa muncul dari content. `EnemyActionPlan` sekarang membawa optional `AppliedStatus` dan `StatusDuration` sehingga intent, Analyze dan eksekusi memakai deskripsi action yang sama.

| Status | Sumber | Durasi | Waktu aplikasi |
| --- | --- | --- | --- |
| Burn | Ashen Fang setelah Basic Attack | 2 enemy turns, max 3 stack | Sesudah Basic Attack berhasil |
| Weakness | Elite: Ashen Crush | 2 turns | Setelah hit Ashen Crush dan Player masih hidup |
| Curse | Boss: Abyssal Rupture | 2 turns | Setelah hit Rupture dan Player masih hidup |

Status dari serangan tidak mengubah damage hit yang menyebabkannya; status mulai memengaruhi aksi setelah hit tersebut. Analyze menampilkan efek status dari prepared plan. Intent Rewrite Weaken tetap mekanik tersendiri dan tidak disamakan dengan status Weakness.

### Kompatibilitas dan integrasi

- Tidak ada scene, Inspector assignment, package, sprite atau perubahan schema SQLite baru.
- Reward switch menerima Frozen Orbit dan Inverted Pulse.
- Save lama tetap memakai graph/checkpoint compatibility sebelumnya. Modifier Anomaly yang valid dipulihkan melalui array modifier yang sudah ada.
- HP selalu diklem setelah perubahan Effective Max HP dan heal Inverted Pulse tidak dapat melewati cap.
- Sistem Elite/Boss lama, fase Boss, Guard, reward, map progression dan checkpoint tetap memakai jalur yang sama.

### Verifikasi final

- `dotnet restore` dan `dotnet build --no-restore`: **0 error**, dengan 4 warning obsolete API lama pada UI/lookup yang tidak berasal dari milestone ini.
- Full Unity Play Mode pada salinan project terisolasi: **110/110 PASS**, **0 failed**, **0 skipped**, durasi **67.3795565 detik**.
- Test baru:
  - `TC_STATUS_003` membuktikan Elite Ashen Crush memberi Weakness dan Boss Abyssal Rupture memberi Curse dengan plan/deskripsi yang sesuai.
  - `TC_ANO_004` membuktikan Frozen Orbit menurunkan Speed, membuat enemy membuka round dan memperpanjang Guard menjadi dua damaging hits.
  - `TC_ANO_005` memakai seed/index yang seharusnya Critical untuk membuktikan Basic Attack Inverted Pulse tetap non-critical dan memulihkan tepat 2 HP.
- Katalog diuji berisi empat Anomaly dan menghasilkan tiga pilihan seeded yang berbeda.
- Delapan SHA-256 file inti workspace cocok dengan salinan yang menghasilkan hasil final; mismatch 0.

Bukti lengkap: `Docs/TestResults/2026-09-23-StatusAnomaly/REPORT.md`, `final-results.xml`, dan `source-hashes.csv`.

Status milestone: **selesai secara fungsional dan lulus regresi otomatis**. Batas klaim: belum ada standalone Windows build, telemetry playtest panjang atau persetujuan final angka balance dari pemilik. Speed 10/9, Critical 5%, multiplier 1,5, heal 2 dan angka Frozen Orbit adalah baseline tuning yang dapat disesuaikan setelah playtest.

Pekerjaan gameplay berikutnya yang direkomendasikan: **Run Summary**. Layar tersebut sebaiknya menampilkan rute yang ditempuh, alasan run berakhir, relic dan Anomaly aktif, statistik penting battle, serta resource akhir. Implementasinya harus membaca state final tanpa mengubah aturan run/checkpoint yang sudah lulus verifikasi.

## Run Summary dan redesign UI hasil run — 23 September 2026

Arahan pengguna: lanjut ke Run Summary dan sekaligus ubah desain UI hasil run lama yang belum mengikuti kualitas visual SYNGRAVA. Perencanaan visual dibuat lebih dahulu di Figma, lalu diterapkan dan diperiksa melalui render Unity aktual.

Desain acuan internal: [Figma Run Summary frame 9:14](https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=9-14), ukuran desain 1660×930. Desain memakai struktur hitam/violet/putih, rail rute, Final Resources, Combat Record, Build Archive, seed, dan satu CTA. Unity membangun tampilan ini saat runtime serta memakai sprite torn-paper, crystal, eye dan ikon node yang sudah tersedia. Tidak ada scene edit atau Inspector assignment baru.

### Tampilan dan alur

- Clear menampilkan `THREAD OF FATE SECURED` / `RUN CLEARED` dengan accent violet.
- Defeat menampilkan `THREAD SEVERED` / `RUN DEFEATED` dengan accent crimson.
- Ringkasan memuat alasan berakhir, jumlah node, rail node yang benar-benar ditempuh, HP/Mana/Embers akhir, statistik combat, Relic, Anomaly, modifier, seed dan tombol `BEGIN NEW JOURNEY`.
- Route copy memakai label pemain seperti `BATTLE`, `EVENT`, `REST`, `ELITE`, `RELIC`, `MERCHANT`, dan `BOSS`. Kode internal seperti `BATTLE_01` tidak ditampilkan.
- Tombol New Journey memakai reset lama yang sudah teruji. Overlay segera disembunyikan ketika reset atau map dibuka kembali.
- Result panel scene lama tetap diisi di belakang overlay untuk menjaga serialized reference dan kompatibilitas regression lama. Pemain hanya melihat Run Summary baru.

### Statistik dan save

`RunManager` sekarang menyimpan Elapsed Time, Battles Won, actual Damage Dealt, actual Damage Taken, actual Healing, Critical Hits, Player Actions, total Convergences, serta jumlah Event Horizon, Reversal Orbit dan Unstable Pulse. Damage/healing memakai nilai yang benar-benar masuk setelah cap sehingga overkill dan overheal tidak membesarkan angka.

Statistik ikut `SeededRunState` schema 3 dan checkpoint JSON. Field tambahan bersifat optional bagi JSON save lama dan default ke nol, jadi tidak ada schema bump. Validator menolak angka negatif/non-finite, Critical melebihi jumlah action, battle count tidak masuk akal, atau total Convergence yang tidak sama dengan jumlah ketiga polanya. Restore yang invalid berhenti sebelum live run berubah. New Run mengembalikan seluruh statistik ke nol.

Elapsed time dihitung memakai unscaled delta selama run aktif, termasuk waktu keputusan di map, dan berhenti ketika Settings membuat `Time.timeScale` nol. Continue mempertahankan durasi/counter dari checkpoint boundary terakhir.

### Verifikasi

- Full isolated Unity Play Mode final: **111/111 PASS**, **0 failed**, **0 skipped**, **65.2219161 detik**.
- `TC_SUM_001` membuktikan round trip JSON seluruh counter, penolakan state counter yang korup tanpa partial mutation, dan reset New Run.
- Regression Clear/Defeat memeriksa title/status baru, route tanpa kode internal, kompatibilitas tombol lama, serta CTA Run Summary.
- Render 1920×1080 dan 1280×1024 menjalankan containment dan TextMesh Pro overflow assertion untuk semua label/button.
- Kedua screenshot final diperiksa visual. Ornamen mata/judul dan Build Archive/route yang sempat terlalu dekat telah dirapikan sebelum final run.
- Enam hash source/test workspace cocok dengan salinan isolated yang menghasilkan hasil final.

Bukti: `Docs/TestResults/2026-09-23-RunSummary/REPORT.md`, `final-results.xml`, `source-hashes.csv`, `run-summary-1920x1080.png`, dan `run-summary-1280x1024.png`.

Batas klaim: Echo Fragments, ScoreCalculator, persistent Run History dan Local Leaderboard belum mempunyai sumber data authoritative. Run Summary saat ini sengaja tidak menampilkan angka palsu untuk fitur tersebut. Belum ada standalone Windows build, audio khusus, telemetry playtest panjang atau owner acceptance final.

Milestone berikutnya menurut urutan yang telah disepakati adalah **Echo Fragments**. Definisikan kontrak permanent currency dan sumber reward lebih dahulu; setelah itu jumlah fragment yang benar dapat ditambahkan ke Run Summary tanpa mencampur permanent progression dengan temporary modifier.

## Echo Fragments — 23 September 2026

Echo Fragment kini memiliki ikon kristal retak ungu/putih transparan asli SYNGRAVA di `Assets/Project/Resources/UI/EchoFragment.png`. Run Summary menampilkan ikon, jumlah yang berhasil disimpan, total saldo, dan rincian sumbernya. Pada kegagalan penulisan saldo, layar memberi pesan yang jelas dan tombol hasil run mencoba menyimpan ulang sebelum New Journey.

### Aturan ekonomi

- Battle biasa memberi **8–12** fragment. Nilai dipilih secara deterministik dari seed dan kode node; melanjutkan checkpoint tidak mengocok ulang hasilnya.
- Battle Elite memberi **20** dan Boss yang dikalahkan memberi **50**. Node nonbattle tidak memberi fragment.
- Run Clear menyimpan semua fragment yang ditemukan dan mendapat **bonus clear 20**. Nilai 20 masih baseline sementara karena GDD menyebut bonus clear tanpa menentukan angkanya.
- Run Defeated menyimpan **60%** fragment yang telah ditemukan dari node battle yang selesai, dibulatkan ke bawah. Contoh: 9 ditemukan → 5 disimpan.
- Fragment dihitung dari riwayat node yang benar-benar diselesaikan. Penyaluran ke dompet permanen dilakukan pada akhir run. Embers dan Temporary Run Stats tetap milik run saja. Fragment belum dapat dibelanjakan; Horizontal Unlock adalah milestone berikutnya.

### Penyimpanan dan pemulihan

`EchoFragmentWallet` menyimpan saldo dan ledger klaim di `Application.persistentDataPath/Saves/syngrava-echo-fragments-v1.json`, terpisah dari checkpoint Active Run. File memakai hash, tulis ke file sementara, dan salinan generasi sebelumnya (`.bak`). File yang rusak ditolak; backup yang valid bisa dibaca. Kegagalan menulis fragment tidak menghapus checkpoint.

Setiap New Run mendapat `RunId` acak yang ikut ke checkpoint schema 3. Klaim untuk ID yang sama hanya berlaku sekali, termasuk bila terminal run diputar ulang setelah recovery. Checkpoint schema 3 lama tanpa `RunId` diberi identitas legacy stabil dari seed dan route version; schema tetap 3 agar save lama dapat dilanjutkan. Identitas ini tidak mengubah seed, graph, reward, atau statistik run.

Verifikasi final Unity Play Mode pada salinan proyek terisolasi: **114/114 PASS, 0 failed, 0 skipped**, durasi 64,2978296 detik. Tes baru mencakup idempotensi, persistensi antarrun, kompatibilitas save lama, bonus Clear/Boss; regression UI membuktikan sprite termuat. Screenshot 1920×1080 dan 1280×1024 diperiksa visual serta lolos assertion overflow/containment. Laporan dan bukti: `Docs/TestResults/2026-09-23-EchoFragments/REPORT.md`.

Batas klaim: belum ada Windows standalone build atau playtest ekonomi panjang. Horizontal Unlock yang memakai saldo ini masih milestone berikutnya.

## Run Summary polish dan Horizontal Unlock pertama — 24 September 2026

Arahan terakhir: perbaiki visual Run Summary yang keluar panel, tingkatkan kontras ikon riwayat node, dan hilangkan informasi footer yang berulang; kemudian lanjutkan pekerjaan milestone berikutnya. Rancangan Run Summary diperbarui pada [Figma frame 9:14](https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=9-14). Rancangan struktur Echo Archive dibuat pada [Figma frame 15:14](https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=15-14). Preview tambahan di Figma dibatasi kuota akun; keputusan akhir dicek lewat render Unity yang tersimpan pada laporan tes.

### Perubahan visual yang diterima ke implementasi

- Headings Final Resources, Combat Record dan Build Archive sekarang masuk ke area panel; rincian Echo dan baris terakhir Combat diberi jarak aman. Assertion containment dan overflow memeriksanya pada 1920×1080 dan 1280×1024.
- Rail riwayat tetap berisi ikon node yang benar-benar ditempuh. Ikon menjadi putih di atas penanda gelap berbingkai violet agar setiap node terbaca, bukan menyatu dengan garis.
- Footer yang mengulang label rute dan menampilkan seed tidak terlihat lagi. Snapshot tetap menyimpan seed/rute untuk save dan tes. Tombol Begin New Journey dipusatkan.
- Echo Archive memakai kartu gelap-violet/putih SYNGRAVA yang terpusat pada menu, dengan teks harga dan status yang tetap berada dalam panel pada kedua resolusi render.

### Aturan Horizontal Unlock v1

1. Echo Fragment adalah satu-satunya currency permanen yang dapat dibelanjakan saat ini. Wallet versi 2 menambahkan ledger pembelian pada ledger klaim run lama. Saldo selalu sama dengan total fragment yang diklaim dikurangi biaya pembelian. Wallet v1 dibaca dan dimigrasikan tanpa membatalkan klaim lama; backup/integrity check lama tetap digunakan.
2. Unlock pertama: **Echo Prism**, ID `RELIC_ECHO_PRISM`, harga awal **120 Echo Fragments**. Pembelian hanya boleh sekali. Saldo kurang, ID tidak dikenal, atau wallet tidak valid tidak boleh menghasilkan pembelian. Harga ini baseline sementara untuk playtest ekonomi.
3. Tombol `ECHO ARCHIVE` pada Main Menu membuka status saldo dan pembelian. Bila checkpoint Active Run masih dapat dilanjutkan, pembelian dinonaktifkan agar daftar Relic tidak berubah di tengah run. Saat Main Menu dimuat kembali setelah Battle, bootstrap UI berjalan melalui callback scene load. EventSystem menu memakai Input System UI module proyek; masalah `StandaloneInputModule` lama diperbaiki.
4. Unlock **memperluas pool kartu Relic Shrine**; tidak memberi relic otomatis, tidak menambah Base Stats, dan tidak mengubah reward node yang telah berkomitmen. Relic yang dipilih aktif selama run seperti empat Relic lama. Echo Prism unik dan memberi +4 damage pada Basic Attack pertama setiap battle, lalu reset saat battle berikutnya. Tidak boleh stack dengan dirinya sendiri.
5. `RunRelicCatalog.GetDefinitions()` tetap mengembalikan empat Relic dasar agar API/test lama stabil; `GetAllDefinitions()` mencakup Echo Prism untuk nama di archive/summary. `CreateChoices` memeriksa ledger unlock, sedangkan `CreateChoicesForUnlockState` memungkinkan tes seeded yang bebas filesystem.

### Bukti dan batas klaim

Full isolated Unity Play Mode final: **118/118 PASS**, **0 failed**, **0 skipped**, 65,9695361 detik. Tes baru memeriksa purchase persistence/idempotence, pool locked/unlocked, damage hanya pada Basic Attack pertama, menu setelah scene load, modul input UI, dan containment/render Echo Archive. Kompatibilitas format wallet v1 diperiksa pada implementasi, tetapi belum mempunyai kasus migrasi v1 tersendiri. Sembilan source/test hash workspace sama dengan salinan proyek yang lulus. Dua pasang screenshot aktual Run Summary dan Echo Archive telah diperiksa pada 1920×1080 dan 1280×1024.

Bukti: `Docs/TestResults/2026-09-24-HorizontalUnlock/REPORT.md`, `final-results.xml`, `source-hashes.csv`, dan empat PNG render. Tidak ada scene/Inspector assignment baru, SQLite schema baru, atau package baru. Belum ada Windows standalone build, sesi playtest panjang, ataupun validasi balance akhir oleh pemilik. Run History dan Leaderboard belum diimplementasikan.

Milestone berikutnya yang disarankan: **Run History lokal** dari snapshot terminal yang final dan idempotent. Setelah struktur simpan/baca dan UI riwayat teruji, ScoreCalculator dapat dirancang terpisah sebelum Local Leaderboard sehingga tidak ada skor semu.

## Run History lokal — 24 September 2026

Arahan pengguna: langsung membuat Run History lokal sebagai langkah berikutnya. Implementasi ini memakai data Run Summary terminal yang sudah ada, tanpa mengubah desain rute node, aturan combat, base stat, atau bentuk checkpoint Active Run.

**Perilaku yang sudah dipasang:**

1. `RunHistoryStore` menyimpan hasil Standard Run Cleared/Defeated pada file lokal `Application.persistentDataPath/Saves/syngrava-run-history-v1.json`. Rekaman memuat `RunId`, waktu UTC, outcome, seed, node terselesaikan, resource akhir, Echo yang diperoleh, durasi, statistik pertempuran, rute, Relic, Anomaly, dan modifier. Urutan baca terbaru dulu; maksimum 10.000 entri. Rekaman berulang dengan `RunId` yang sama tidak ditulis lagi.
2. Arsip memakai JSON dengan pemeriksaan SHA-256, temporary write, dan backup. Bila primary rusak, pembacaan mencoba backup. Data rusak ditolak. UI membaca salinan data sehingga pemilihan entri tidak mengubah file save.
3. `BattleManager` mencatat run terminal **setelah klaim Echo sukses dan sebelum checkpoint Active Run dihapus**. Jika penulisan riwayat gagal, Run Summary menampilkan pesan dan tombol untuk mencoba lagi; checkpoint tetap tersedia selama percobaan. Saat New Journey dipilih setelah retry berhasil, reset berjalan seperti sebelumnya. Active Run, Echo wallet, dan History tetap file terpisah.
4. Main Menu mempunyai tombol `RUN HISTORY`, membuka panel `THREAD ARCHIVE` read-only. Daftar enam hasil per halaman; detail memperlihatkan tanggal, seed, durasi, resource, battle, build dan rute. Panel gelap/violet/putih dengan aksen merah untuk Defeat. Rute panjang dibagi menjadi dua baris agar tidak ada separator yang tertinggal sendiri. UI dibuat runtime; tidak ada Inspector assignment atau scene migration.

**Verifikasi:** run final seluruh `BattleSnapshotTests` pada proyek Unity terisolasi **122/122 PASS**, 0 gagal, 0 skipped. Tes baru memeriksa persistensi, idempotensi, backup, penolakan write path yang tidak dapat ditulis, integrasi akhir battle/checkpoint, menu, pagination, batas teks, dan render 1920×1080 serta 1280×1024. Kedua render terakhir diperiksa secara visual. Dua kegagalan antara berasal dari asersi tes combat lama yang mengabaikan peluang critical hit acak; tesnya diperbaiki tanpa mengubah aturan damage, dan run final hijau. Sembilan source/test hash workspace identik dengan salinan tes. Bukti lengkap: `Docs/TestResults/2026-09-24-RunHistory/REPORT.md`, `final-results.xml`, `source-hashes.csv`, dan dua screenshot.

**Batas:** belum diuji pada Windows standalone atau sesi main manual lengkap. Pemulihan backup dapat kehilangan paling banyak commit terbaru bila primary rusak tepat setelah commit itu. Fitur ini belum mempunyai score, filter, penghapusan, cloud sync, atau leaderboard. Milestone berikutnya yang disarankan: definisikan `ScoreCalculator` yang adil dan deterministik dari hasil run yang sudah tercatat; tampilkan score pada riwayat setelah formula disepakati, baru buat Local Leaderboard. Jangan menyebut Run History sebagai leaderboard saat ini.

## Main Menu dua area — 24 September 2026

Setelah Run History, pengguna meminta beralih ke Main Menu. Prioritas rinci belum diberikan pada awal implementasi; perbaikan dilakukan pada hierarki visual dan kejelasan alur tombol, sambil mempertahankan perilaku save, unlock, history, dan settings yang sudah teruji. Rencana desain ada di `Docs/MainMenu_DesignPlan.md`.

- Panel runtime sekarang berukuran 1450×820 dan terbagi menjadi identitas di kiri serta navigasi di kanan. Kiri menampilkan judul SYNGRAVA, ASHEN CYCLE, tagline, dan sprite mata transparan `StandardRunMapVisualAssets.CurrentMarker`; tepi memakai ornamen retak yang sudah tersedia. Kanan menempatkan Continue Journey dan New Expedition sebagai dua tindakan utama, kemudian Echo Archive, Run History, Settings, dan Quit Game dalam dua baris ringkas.
- Continue tetap membaca `ActiveRunCheckpointStore`: tanpa checkpoint tombol tidak aktif dan memberi status jelas; dengan checkpoint tombol aktif dan merangkum stage, HP, Mana, serta Embers. New Expedition tetap meminta konfirmasi bila akan mengganti Active Run. Semua handler lama dipertahankan. Tidak ada scene, prefab, aset, package, atau Inspector assignment baru.
- Render Unity pada 1920×1080 dan 1280×1024 diperiksa. Crop mata atlas yang memperlihatkan batas kotak diganti dengan sprite CurrentMarker resmi. Tes baru memeriksa containment teks/tombol, dua render, state Continue tanpa/dengan checkpoint, dan pembatalan konfirmasi New Run.
- Verifikasi final seluruh suite `BattleSnapshotTests` pada salinan proyek terisolasi: **124/124 PASS**, 0 gagal, 0 skipped. Source dan test yang berubah cocok dengan salinan Unity berdasarkan SHA-256. Bukti: `Docs/TestResults/2026-09-24-MainMenu/REPORT.md`, `final-results.xml`, dua PNG, dan `source-hashes.csv`.
- Keterbatasan desain: file Figma SYNGRAVA belum mempunyai frame Main Menu, dan pembacaan detail lanjutan diblokir kuota panggilan MCP paket Starter. Pencarian Canva untuk desain SYNGRAVA milik pengguna tidak menemukan hasil. Implementasi mengikuti visual yang sudah tersedia di proyek; render Unity adalah bukti utama, bukan persetujuan desain final dari pemilik.

Langkah berikutnya setelah pemilik meninjau Main Menu: terapkan koreksi visual/teks yang diminta, lalu kembali ke `ScoreCalculator` dan Local Leaderboard bila Main Menu sudah diterima. Belum ada Windows standalone build atau tes manual pada aspek rasio ekstrem.

## Standard Run ScoreCalculator — 25 September 2026

Arahan pengguna berikutnya adalah melanjutkan progres sesudah Main Menu. Implementasi dilanjutkan ke `ScoreCalculator`, milestone yang sudah tercatat setelah Run History. Rencana dan batas skor ditulis sebelum coding di `Docs/Score_Model.md`; rumus dasar diambil dari GDD dan dipakai dengan multiplier 1 untuk Standard Run. Ini belum merupakan persetujuan balance final.

1. Skor model 1 memakai node yang diselesaikan: Normal Battle +100, Random Event +75, Elite +500, Boss +1.500. Named Convergence +50 hanya untuk tiga pemicu pertama **pada setiap battle yang akhirnya selesai**. Rest, Merchant, Relic, damage, healing, waktu, Embers, Echo, dan turn yang dipanjangkan tidak memberi poin. Battle yang kalah tidak mengunci poin node atau Convergence-nya. `ScoreCalculator` murni menghasilkan `ScoreResult` dengan total dan lima komponen, tanpa akses UI, randomizer, disk, atau save service.
2. `RunManager` melacak `ScoreableConvergences` yang sudah committed dan `CurrentBattleConvergences` yang dibatasi tiga. Dua field ini ikut tersimpan pada `SeededRunState` schema 3 dan divalidasi saat restore. Checkpoint schema 3 lama tetap dapat dibaca; karena tidak memuat breakdown per battle lama, field skor baru mulai dari nol, bukan diisi tebakan.
3. `RunSummarySnapshot` membawa hasil kalkulasi; `RunSummaryUI` menampilkan total di header kanan. `RunHistoryRecord` baru menyimpan versi model, total, dan breakdown; `RunHistoryUI` menunjukkan semuanya. Arsip v1 lama tetap terbaca dan menampilkan `UNSCORED` tanpa rekonstruksi palsu. Tidak ada scene, prefab, Inspector assignment, paket, ataupun skema SQLite baru.
4. Bukti: `Docs/TestResults/2026-09-25-ScoreCalculator/REPORT.md`, dua XML Play Mode dan empat PNG render. Dua full run terakhir **127/127 PASS** masing-masing; final source/test hash cocok dengan salinan Unity yang diuji. Kompilasi kode game 0 error dan kompilasi assembly tes 0 error. Render Run Summary dan Run History pada 1920×1080 dan 1280×1024 diperiksa secara visual.

Batas klaim: tidak ada Windows standalone build atau playtest manual penuh; angka skor masih baseline GDD yang dapat dituning setelah permainan nyata. Infinite Casual/Ranked, difficulty multiplier, dan Local Leaderboard belum ada. Milestone berikutnya adalah Local Leaderboard top 10 yang mengambil data final dari Run History dan memakai tie-break depth, boss count, lalu durasi; jangan menyebut Run History saat ini sebagai leaderboard.

## Local Leaderboard dan integrasi Main Menu — 25 September 2026

Pengguna meminta melanjutkan progres yang tertunda. Setelah ScoreCalculator selesai, implementasi berlanjut ke Local Leaderboard yang sudah tercatat sebagai milestone berikutnya. Rencana dibuat di `Docs/LocalLeaderboard_DesignPlan.md` sebelum UI diprogram.

### Perilaku yang sudah berjalan

1. Main Menu mempunyai tombol **TOP RUNS**, di bawah empat tombol utility. Tombol ini membuka **FATE LEDGER**, tabel sepuluh Standard Run terbaik dari Run History lokal. Kolom terpisah memuat rank, Cleared/Defeated, score, depth, boss count, dan duration. Memilih atau memfokuskan baris menampilkan seed, tanggal selesai, ruleset dan breakdown poin pada bagian bawah. Teks tidak disusun dengan spasi manual untuk meniru tabel.
2. `LocalLeaderboardService` membaca seluruh arsip (batas store 10.000), memfilter model skor saat ini/Standard Run, menolak RunId ganda, lalu mengambil sepuluh terbaik. Tie-break adalah score menurun, node selesai menurun, boss count menurun, durasi menaik, tanggal selesai menaik, lalu RunId ordinal. Hasil peringkat berupa salinan nilai yang tidak ikut berubah bila record asal diubah. Standard Run saat ini cycle 1; cycle disimpan sebagai metadata untuk penggunaan berikutnya.
3. `RunHistoryRecord` menambah metadata optional `RulesetVersion`, `Mode`, dan `Cycle` tanpa mengganti format arsip v1. Riwayat sebelum skor tetap UNSCORED dan tidak masuk peringkat; riwayat Standard yang sudah mempunyai skor tetapi belum memiliki metadata baru tetap masuk dengan skor aslinya. Ruleset lama yang tidak tercatat ditampilkan UNKNOWN, tidak ditebak.
4. Leaderboard hanya membaca. Membuka, memilih, atau menutupnya tidak menulis hasil, checkpoint atau wallet. Arsip kosong memberi pesan NO SCORED RUNS YET; kegagalan membaca memberi ARCHIVE UNAVAILABLE tanpa menghapus file. Fokus keyboard berwarna violet gelap, navigasi berada dalam panel, dan Cancel/Close mengembalikan fokus ke tombol pembuka.

### Bug yang ditemukan dan verifikasi

Run pertama menghasilkan **128/129 PASS**. Tes penyimpanan hasil terminal menemukan bug kompatibilitas: `JsonUtility` mengubah field mode legacy yang belum ada menjadi string kosong ketika arsip ditulis bersama hasil baru. Validator awal menolak bentuk kosong sehingga pembacaan kembali ke backup dan hasil terakhir tidak terlihat. Validator dan filter sekarang menerima null/kosong sebagai mode legacy Standard; mode nonkosong selain STANDARD tetap ditolak/difilter sesuai tanggung jawabnya.

Setelah perbaikan, full Unity Play Mode final menghasilkan **130/130 PASS**, 0 failed, 0 skipped, **70.4795756 detik**. `TC_LDB_001` memeriksa pengurutan/tie-break, duplicate identity, filter mode/model dan salinan data. `TC_LDB_002` memeriksa UI, daftar kosong/rusak, fokus/Cancel, containment dan overflow pada dua resolusi, serta kesamaan byte file sebelum/sesudah dibaca. `TC_LDB_003` memastikan arsip tanpa metadata leaderboard dapat ditambah dua hasil baru dan dibuka kembali tanpa kehilangan entri atau mengubah skor lama. Tes integrasi terminal yang sempat gagal kini lulus.

Keempat render Main Menu/Leaderboard pada 1920×1080 dan 1280×1024 diperiksa visual. Warna fokus yang awalnya putih dan menyamarkan teks sudah diganti. Sebelas hash script sumber/tes cocok dengan salinan isolated Unity. Tidak ada error kompilasi C#; empat warning CS0618 lama untuk FindFirstObjectByType masih tercatat. Bukti lengkap: `Docs/TestResults/2026-09-25-LocalLeaderboard/REPORT.md`, dua XML, empat PNG, `compilation-warnings.txt`, dan `source-hashes.csv`. Angka pada screenshot leaderboard adalah fixture tes, bukan playthrough manual.

Tidak diperlukan Inspector assignment, scene/prefab migration, package, SQLite schema, atau aset gambar baru. Tidak ada desain Figma/Canva baru pada kelanjutan ini; layout mengikuti rencana dan visual proyek yang sudah tersedia. Tidak ada commit/push.

### Batas dan pekerjaan selanjutnya

Local Leaderboard saat ini hanya Standard Run lokal. Tidak ada Infinite, online ranking, anti-cheat, filter mode tambahan, atau klaim balance akhir. Belum ada Windows standalone build maupun tiga Standard Run manual berturut-turut. Pekerjaan selanjutnya adalah memverifikasi alur Main Menu → run → terminal summary/history/leaderboard, Continue/Recovery, serta reset build saat Clear/Defeat/Abandon pada Windows. Penuhi gerbang Standard Run yang sudah ditulis sebelum memulai Infinite Casual.

## Premium Main Menu dan Loading Screen di scene — 25 September 2026

Arahan terbaru: melanjutkan desain Figma premium dan memasukkan Main Menu ke scene `MainMenu` yang sudah dibuat pengguna. Rencana visual, palet, komposisi, prompt gambar, tautan Figma/Canva, serta batas penggunaan aset tercatat di `Docs/MainMenu_Loading_Premium_DesignBrief.md`. Figma memuat dua frame 1920×1080 (Main Menu dan Loading Screen). Dua gambar orisinal tanpa teks berada di `Assets/Project/Resources/UI/MainMenuThreshold.png` dan `LoadingVoid.png`.

- `MainMenu.unity` sekarang benar-benar menyimpan Canvas, EventSystem Input System, latar, panel, judul, teks, dan tujuh tombol navigasi sebagai hierarki scene yang terlihat di Editor. `MainMenuUI` memakai objek scene ini ketika Play Mode, menghubungkan handler tombol, dan mengisi status checkpoint/Echo secara dinamis. Dialog konfirmasi, Archive, History, Leaderboard, Settings, dan loading overlay tetap diinisialisasi saat runtime sesuai kebutuhannya.
- Continue hanya aktif bila `ActiveRunCheckpointStore` menemukan state yang valid. Tombol itu menampilkan loading dan membawa pemain kembali ke batas map dari checkpoint. New Expedition menampilkan konfirmasi bila ada save aktif, lalu loading dan run baru. Echo Archive, Run History, Top Runs, Settings, dan Quit menuju fungsi yang sudah ada. `EditorBuildSettings.asset` mencantumkan MainMenu dan Battle sehingga `LoadSceneAsync("Battle")` benar-benar dapat dijalankan; entri SampleScene yang tidak ada telah dibuang.
- Loading screen memakai gambar gelap, segel mata, judul, serta progress bar dari `AsyncOperation.progress`; scene Battle diaktifkan sesudah siap dan jeda visual singkat. Jika loading tidak bisa dimulai, ada pesan gagal, Retry, dan Back to Menu. Kesalahan `RectTransform` pada wadah tombol gagal ditemukan lewat tes Play Mode dan diperbaiki sebelum regresi akhir.
- Verifikasi akhir pada salinan Unity terisolasi: **133/133 Play Mode PASS**, 0 failed, 0 skipped. Lima tes fokus menu mencakup dua resolusi, containment/overflow, Continue, New Expedition lewat loading, konfirmasi save, dan Settings. Dua screenshot Unity pada 1920×1080 dan 1280×1024 diperiksa secara visual. Tujuh hash scene/source/aset workspace identik dengan salinan yang diuji. Bukti: `Docs/TestResults/2026-09-25-PremiumMainMenu/REPORT.md`, XML, PNG, dan CSV hash.

Batas klaim: belum ada Windows standalone build atau sesi manual penuh, dan gambar final/motion belum disetujui pemilik. Figma adalah wireframe komposisi, bukan ekspor art produksi. Setelah penerimaan visual, lanjutkan uji end-to-end menu → run → summary/history/leaderboard serta gerbang Standard Run sebelum fitur Infinite.

## Penggantian visual tombol Main Menu — 25 September 2026

Permintaan terbaru adalah mengganti gambar/desain tombol pada Main Menu yang sudah berada di scene, sambil mempertahankan fungsi masing-masing. Satu gambar orisinal transparan `Assets/Project/Resources/UI/MenuButtonFracturedFrame.png` dibuat dengan tema obsidian retak, aksen ungu tipis, dan pusat gelap yang tetap tenang untuk teks. Sprite tersebut dipasang pada tujuh tombol scene: Continue Journey, New Expedition, Echo Archive, Run History, Settings, Quit Game, dan Top Runs. Setiap tombol masih memakai `Button` Unity serta label TextMeshPro hidup; warna interaksi normal, hover/fokus, pressed, dan disabled kini menint gambar yang sama. Kode pembangun UI cadangan di `MainMenuUI.cs` memakai sprite dan tata letak yang sama.

QA visual pertama menemukan helper copy terlalu dekat dengan garis bawah frame. Posisi label/subteks kemudian digeser ke bagian dalam pada scene dan pembangun UI cadangan; helper copy pada tombol Top Runs yang pendek dihapus. Render Unity final pada **1920×1080** dan **1280×1024** diperiksa kembali: tujuh tombol tetap berada dalam panel, teks tidak lagi menumpuk frame, dan artwork latar tetap menjadi fokus sisi kanan.

Tes fokus menu setelah perbaikan: **5/5 PASS**. Full Unity Play Mode pada salinan proyek terisolasi: **133/133 PASS**, 0 gagal, 0 skip. Kode UI, PNG sprite, metadata sprite, dan tes sama persis secara SHA-256 antara workspace dan salinan uji. Scene sama setelah normalisasi akhir baris/spasi; perbedaan hash mentah hanya karena pembersihan whitespace pada salinan workspace setelah scene disimpan oleh Unity. Bukti XML, dua screenshot, hash, dan laporan ada di `Docs/TestResults/2026-09-25-MainMenuButtons/`. Tidak ada Windows standalone build atau playtest manual menyeluruh pada iterasi visual ini. Aset tombol adalah satu plate dengan tint state, bukan empat gambar state terpisah atau 9-slice. Tidak ada commit/push dan perubahan pengguna yang sudah ada tidak direset.

## Integrasi Standard Run dan Windows build — 26 September 2026

Setelah Main Menu diterima untuk dilanjutkan, pekerjaan berikutnya adalah gerbang Standard Run yang tercantum pada bagian **Infinite Run and Leaderboards**. Tidak ada gameplay baru atau perubahan balance pada iterasi ini. Dua tes Play Mode baru memeriksa alur antarscene dan transaksi state yang sebelumnya belum terbukti secara menyeluruh.

1. `TC_E2E_001` memulai New Expedition dari Main Menu, memilih tombol node pada route yang terhubung sampai boss, memuat ulang Main Menu setelah dua node, lalu Continue. RunId, depth, dan panjang jalur kembali identik. Setelah Clear, Summary terbuka, Echo tersimpan, satu History record tercatat, checkpoint terhapus, hasil masuk Top Runs, dan Continue tidak aktif. Run baru memperoleh RunId lain pada depth 1 tanpa modifier lama.
2. `TC_E2E_002` memeriksa New Expedition ketika checkpoint aktif dan temporary modifier telah diperoleh. Konfirmasi mengganti checkpoint; run baru tidak mewarisi modifier atau jalur run lama.
3. Tes fokus **2/2 PASS**; seluruh `BattleSnapshotTests` **135/135 PASS**, 0 gagal, 0 skip, 80,130 detik. Tes berjalan pada salinan Unity terisolasi dengan identitas produk terpisah. Enam hash scene/source/build-setting sama antara workspace, salinan tes, dan salinan build pada saat diuji. Detail dan XML: `Docs/TestResults/2026-09-26-StandardRunGate/REPORT.md`.
4. Windows x86_64 Development Build pada Unity `6000.6.0f1` **berhasil**, ukuran laporan BuildPipeline 332.420.599 byte, 736,97 detik, **0 error**, **514 warning**. Empat lokasi kode memakai API `FindFirstObjectByType` yang deprecated; mayoritas warning lain berasal dari varian shader/compute paket AI. Executable berada di `Builds/Verification-2026-09-26/SYNGRAVA.exe`; log lengkap ada dalam folder yang sama. Build lama di `Builds/Windows` tidak ditimpa.
5. Startup tersembunyi batchmode selama 55 detik melewati inisialisasi engine, Direct3D 12, dan reload assembly tanpa exception/error pada log. Karena log tidak memastikan scene Main Menu selesai muncul dan tidak ada inspeksi layar/input, startup visual/interaksi tetap **belum terverifikasi**.

**Batas klaim:** tes integrasi mempercepat penyelesaian encounter dengan `EndBattle(true)` supaya fokus pada map, scene, dan transaksi save. Itu bukan tiga playthrough combat manual. Tes Defeat/Recovery terpisah sudah ada di regresi, tetapi perlu pembuktian ulang pada executable interaktif. Gerbang Infinite Casual tetap tertutup sampai tiga Standard Run berturut-turut tanpa blocker, reset temp build setelah kemenangan/kekalahan/abandon, resume state yang sama, dan skor deterministik diverifikasi sesuai `Docs/StandardRun_ReleaseGate_Checklist.md`. Jangan menandai tahap ini selesai hanya dari build atau XML. Tidak ada commit/push; seluruh perubahan pengguna yang sudah ada dipertahankan.

## Audit Skill Archive dan rancangan Infinite Run — 15 November 2026

Roster runtime telah diaudit ulang: **30 skill memiliki active, passive, dan deskripsi passive yang masing-masing unik**. Perbaikan menyesuaikan efek runtime dengan tooltip, termasuk Cinder Key, Abyssal Verdict, Gravity Thread, Paper Crown, dan Rift Siphon pada kill tidak langsung. Nilai damage/Mana masih baseline; belum disahkan lewat playtest balance pemilik.

Regresi Play Mode pada salinan terisolasi lulus tiga kali berurutan: **168/168 PASS, 0 failed, 0 skipped pada setiap pass**. Source workspace sama dengan source yang diuji pada delapan SHA-256. Laporan, profil, XML, log, dan hash berada di `Docs/TestResults/2026-11-15-SkillSystem/REPORT.md`. Ketiga pass adalah uji regresi otomatis, bukan tiga playthrough manual.

Rancangan riset untuk milestone sesudah gerbang Standard Run ditulis di `Docs/Infinite_Run_DesignPlan.md` dan diperkuat dengan video developer resmi Hopoo Games **Dev Thoughts #23**, patch anti-stall Simulacrum, serta patch Endless Mode *Vampire Survivors* v1.0. Prinsip hadiah pilihan, boss berkala, pengulangan Cycle, dan anti-soft-lock dicatat sebagai pola rujukan; angka dan konten tidak disalin. Audit tautan unit rarity tinggi yang diminta pemilik kembali menemui HTTP 403 pada Vanguards/Anime Expeditions dan halaman 32x.dev tanpa teks; batas akses serta ringkasan pola sekunder tercatat di `Docs/Skill_Collection_Loadout_DesignPlan.md`. Infinite belum diimplementasikan. Gerbang rilis Standard Run pada `Docs/StandardRun_ReleaseGate_Checklist.md` tetap prasyarat.
