# Rancangan Skill Archive, Loadout, dan Reward Run — SYNGRAVA

**Tanggal:** 15 November 2026  
**Status:** Implemented; 30 distinct active/passive pairs refined and verified by three consecutive 168/168 Play Mode regression passes on 15 November 2026. Balance remains provisional pending owner playtest.  
**Cakupan dokumen:** koleksi skill permanen, pilihan skill sebelum Run, reward skill dari Run, dan rancangan roster 30 skill.

## 1. Tujuan

Pemain dapat membuka skill resmi baru melalui permainan, menyimpannya di Skill Archive, lalu mengatur sendiri skill mana yang dibawa pada Run berikutnya. Isi skill tetap dibuat dan diseimbangkan oleh SYNGRAVA; pemain memilih dari katalog yang disediakan game.

Perkembangan ini bersifat horizontal: semakin banyak Run, semakin banyak cara membangun strategi. HP, Mana awal, Attack, dan Base Stats Warden tetap sama untuk semua pemain.

## 2. Kondisi proyek saat ini

- Battle memuat hingga tiga skill dari loadout profil yang disalin ke snapshot Run. `SKL_SHADOW_STRIKE` menjadi skill awal.
- `SkillData` menyimpan kode, nama, biaya Mana, Power, target, jenis efek, deskripsi, dan asset key. Database seed `game_seed.db` saat ini berisi satu skill, Shadow Strike.
- Koleksi, loadout, passive, reward Skill Memory, dan UI Skill Archive kini tersedia. Roster runtime berisi 30 skill kurasi.
- `ActiveRunCheckpointStore` menyimpan Run aktif sebagai JSON ber-checksum; `SeededRunState` memakai schema 4 dan memigrasikan schema 3 dengan Shadow Strike pada slot pertama.
- Echo, Run History, dan checkpoint sekarang memakai store lokal masing-masing. `UserSettingsStore` memakai SQLite terpisah.
- `SkillContentRepository` dan Content Manager adalah alat CRUD internal. Baris `SKL_CUSTOM_*` tidak boleh masuk ke battle, reward, koleksi, atau loadout pemain.
- GDD v0.5 dan keputusan pemilik menetapkan katalog kurasi, Base Stats terkunci, serta loadout dengan jumlah slot tetap. Keputusan terbaru memberi pilihan satu skill permanen setelah Standard Run yang memenuhi syarat.

## 3. Riset dan keputusan desain

Game8 Chaos Zero Nightmare yang ditautkan pemilik sudah dicoba dibuka, tetapi server mengembalikan **402 Payment Required**. Sebagai gantinya, rancangan memakai catatan developer resmi Chaos Zero Nightmare serta panduan publik yang dapat dibaca. CZN membangun Save Data dari pilihan pemain dalam batas kapasitas dan membedakan aturan data untuk mode kompetitif; ide yang diambil untuk SYNGRAVA adalah pilihan yang jelas, data yang tetap milik pemain, dan pemisahan aturan mode. Sistem kapasitas kartu, equipment, season, atau timnya tidak disalin. [Catatan developer CZN tentang Save Data dan Merit Battle](https://czn.gg/%E2%97%A4developer-notes-new-combatant-blazebell-new-great-rift-boss-and-more%E2%97%A2/), [panduan Save Data CZN](https://gamewith.net/chaoszeronightmare/72521)

*Slay the Spire II* memisahkan karakter/pool pilihan, build sementara yang berubah antar-Run, dan progres Timeline yang terbuka lewat percobaan bermain. Ide yang dipakai adalah memisahkan kepemilikan skill permanen dari skill yang sedang dibawa Run. Informasi STS2 masih berubah selama Early Access, jadi detail balance-nya bukan patokan. [Halaman resmi Slay the Spire 2 di Steam](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/), [ringkasan sistem Timeline dan Run](https://en.wikipedia.org/wiki/Slay_the_Spire_II)

### Riset passive dan active unit raritas tinggi

Tautan unit yang diberikan pemilik sudah dibuka langsung. `wiki.vanguards.gg` dan `wiki.animeexpeditions.com` menolak akses dengan HTTP 403; halaman daftar unit 32x.dev dapat dimuat tetapi tidak menyediakan isi teks yang bisa dianalisis. Karena itu, rincian individual dari ketiga halaman tersebut tidak diasumsikan. Pencarian sekunder menemukan ringkasan kemampuan Anime Expeditions dan aturan umum Anime Crusaders: unit dengan raritas tertinggi cenderung mendapat passive dan active yang memiliki trigger serta peran khusus. Nilai dan isi guide komunitas dapat berubah karena update. [Anime Expeditions: passive dan active per raritas (All Things How)](https://allthings.how/anime-expeditions-every-unit-passive-and-active-ability-by-rarity/), [Anime Expeditions: daftar ability (Pro Game Guides)](https://progameguides.com/roblox/all-anime-expeditions-unit-passives-active-abilities/), [Anime Crusaders: unit dan passive berdasarkan raritas (BloxInformer)](https://bloxinformer.com/wikis/anime-crusaders/units/)

Pola yang dipakai sebagai pelajaran desain—tanpa menyalin nama atau angka ability—adalah memberi setiap karakter identitas mekanik sendiri, memasangkan active yang bisa dipilih dengan passive yang bereaksi pada kondisi spesifik, dan membuat pemicu itu terbaca bagi pemain. Roster SYNGRAVA menerjemahkannya menjadi 30 pasangan unik dalam sistem turn-based: Burn/Weakness/Curse, Analyze, Guard, HP, Mana, Ember, status cleanse, kill, serta Convergence. Passive terbatas satu kali per battle kecuali definisinya menyebut respons pasif berulang; efek yang memerlukan kondisi gagal tidak terpakai. Semua angka tetap provisional sampai playtest balance.

Di *Hades II*, Arcana dipilih sebelum memulai Run dan dibatasi kapasitas Grasp. SYNGRAVA mengambil tahap persiapan sebelum Run, tetapi jumlah slot skill diusulkan tetap dan tidak bisa dinaikkan dengan mata uang, sesuai aturan Base Stats serta loadout MVP SYNGRAVA. [Penjelasan Grasp dan loadout Arcana Hades II](https://www.shacknews.com/article/146102/upgrade-grasp-hades-2)

### Keputusan yang direkomendasikan untuk SYNGRAVA

1. **Tiga slot Skill tetap per Run.** Slot 1 dimulai dengan Shadow Strike. Slot 2 dan 3 langsung tersedia tetapi kosong sampai pemain memiliki skill lain. Slot tidak pernah dibeli atau ditingkatkan.
2. **Satu loadout aktif untuk MVP.** Pemain dapat mengganti isi tiga slot sebelum Expedition baru. Preset bernama dapat ditambahkan kemudian tanpa menambah jumlah skill tempur.
3. **Satu pilihan skill permanen per Standard Run yang berakhir**, baik Clear maupun Defeat, bila minimal satu node sudah diselesaikan. Run Abandoned tidak memberi skill. Ini membuat kekalahan tetap dapat memberi perkembangan tanpa memberi hadiah hanya karena membuka lalu menutup Run.
4. **Mode wave-based di masa depan** dapat memakai Skill Memory pada checkpoint. Implementasi saat ini memberi reward pada Standard Run terminal.
5. **Skill reward tidak memakai Echo Fragments.** Setiap Run yang memenuhi syarat memberi pilihan untuk membuka satu skill baru secara langsung. Echo tetap dipakai untuk unlock horizontal lain seperti Relic, Archive, atau preset. Ini pilihan yang paling langsung memenuhi permintaan “tiap Run bisa menyimpan skill baru” tanpa membuat skill bergantung pada saldo.
6. Shadow Strike menjadi skill awal yang dimiliki dan terpasang agar tutorial serta Run lama tetap bekerja.

> Keputusan pemilik: skill permanen diperoleh melalui Skill Memory setelah Standard Run yang memenuhi syarat, tanpa biaya Echo. Keputusan ini menggantikan paragraf ekonomi skill sebelumnya pada GDD v0.5 dan telah diimplementasikan.

## 4. Alur pemain

### Persiapan Run

`Main Menu → Echo Archive → Skill Archive → pilih sampai 3 skill → New Expedition`

- Skill Archive memiliki tab **COLLECTION** dan **LOADOUT**.
- Collection menampilkan skill yang dimiliki, skill terkunci, pencarian/filter sederhana, biaya Mana, deskripsi efek, passive, role, dan ikon.
- Skill terkunci dapat dilihat tetapi tidak dapat dipasang. UI menjelaskan cara membukanya: “Pilih sebagai Skill Memory setelah Run yang memenuhi syarat.”
- Pemain memasang skill ke Slot 1–3 dengan tombol **EQUIP TO SLOT**. Drag-and-drop tidak menjadi syarat MVP.
- Satu kode skill hanya boleh dipasang pada satu slot. Slot boleh kosong.
- Pemain menyimpan perubahan dengan tombol **SAVE LOADOUT**. Loadout tidak berubah bila penyimpanan gagal; UI menampilkan pesan gagal.
- Jika ada Active Run, loadout Run tersebut tetap memakai snapshot saat Run dimulai. UI menunjukkan “LOADOUT RUN AKTIF TIDAK BERUBAH” agar pemain memahami bahwa edit hanya berlaku untuk Expedition baru.

### Battle

- Command HUD menampilkan maksimal tiga tombol Skill dari snapshot Run, beserta nama singkat, ikon, dan biaya Mana.
- Pemain hanya boleh menjalankan satu aksi dalam satu giliran. Semua skill tetap termasuk kategori `Skill` untuk Orbit, termasuk heal, Guard, dan debuff.
- Skill sukses membayar Mana tepat satu kali, menerapkan efek, lalu mengisi tepat satu slot Orbit dan mengakhiri giliran pemain.
- Mana tidak cukup, target tidak valid, skill tidak ada di katalog, atau Run tidak mengizinkan input: tidak mengubah Mana/HP/status/Orbit/turn dan tidak menggunakan passive.
- Skill yang punya efek pada intent hanya boleh mendapat bonus bila Analyze sudah mengungkap intent tersebut. Skill tidak membocorkan intent tersembunyi.

### Penutupan Run dan Skill Memory

- Standard Run Clear atau Defeat setelah setidaknya satu node selesai menyiapkan satu Skill Memory.
- Run Abandoned tidak memberi Skill Memory. Penyelesaian callback yang sama tidak membuat hadiah kedua.
- Pemain melihat tiga kartu pilihan dari skill resmi yang belum dimiliki. Jika sisa skill kurang dari tiga, tampilkan semuanya tanpa pengganti duplikat.
- Pilihan yang tersedia dibuat deterministik dari `RunId`, checkpoint/milestone, dan versi katalog, lalu disimpan sebelum ditampilkan. Reload atau membuka kembali Summary menampilkan pilihan yang sama; tidak ada tombol reroll.
- Pemain memilih satu skill. Skill itu dicatat sebagai milik permanen, dihubungkan ke Run sumber, lalu muncul di Skill Archive. Offer yang belum dipilih tetap pending dan dapat dibuka kembali dari Main Menu.
- Hadiah di Standard Run berlaku untuk loadout **Run berikutnya**, karena Run saat ini sudah berakhir. Di mode 10-wave masa depan, skill yang dipilih juga dapat ditambahkan atau ditukar ke build Run aktif; perubahan itu tidak mengganti loadout permanen profil.
- Bila semua skill katalog sudah dimiliki, Summary menampilkan “SKILL ARCHIVE COMPLETE”; tidak ada skill palsu atau duplikat. Reward Run lain tetap berjalan normal.

## 5. Katalog final 30 skill dan identitas mekanik

Semua nilai damage/Mana masih **provisional** dan mengikuti aturan skala yang sama. Setiap kode memiliki `ActiveEffect` dan `PassiveEffect` unik di katalog; daftar ini mencatat implementasi yang sedang diverifikasi, menggantikan draf awal pada [`Skill_Design_Catalog_Draft.md`](Skill_Design_Catalog_Draft.md).

| No. | Skill | Active | Passive unik |
|---:|---|---|---|
| 1 | Shadow Strike | 2 Mana, 20 damage. | Setelah Analyze, Shadow Strike pertama mendapat +4 damage. |
| 2 | Ashwake Canticle | 1 Mana, 6 damage dan 1 Burn selama 2 turn musuh. | Tick Burn pertama dalam battle memulihkan 1 Mana. |
| 3 | Gravemute Seal | 2 Mana, 4 damage dan Weakness 2 turn. | Hit pertama dari musuh yang sedang Weakness dikurangi 3 damage. |
| 4 | Vesper Stitch | 3 Mana, pulihkan 8 HP dan cleanse satu Weakness/Curse. | Biaya skill ini -1 saat HP <= 40%, sekali per battle. |
| 5 | Pale Ledger | 2 Mana, 6 damage dan Curse 2 turn. | Event Horizon pertama saat target Cursed memulihkan 1 Mana. |
| 6 | Blackglass Parry | 3 Mana, 5 damage dan siapkan parry 50% untuk hit langsung berikut. | Reversal Orbit pertama memberi Weakness 1 turn. |
| 7 | Hollow Conduit | 2 Mana, pulihkan 4 Mana. | Unstable Pulse pertama memulihkan 1 Mana tambahan. |
| 8 | Cinder Refrain | 2 Mana, 12 damage; +4 bila target Burning. | Serangan pertama apa pun ke target Burning mendapat +2 damage. |
| 9 | Rift Siphon | 4 Mana, 14 damage; pulihkan 4 HP bila target Cursed. | Kill pertama terhadap target Cursed memulihkan 4 HP. |
| 10 | Blackstar Brand | 2 Mana, 7 damage dan tandai target; Basic Attack berikut +8. | Bila target bertanda selamat dari hit pertama, target mendapat Weakness 1 turn. |
| 11 | Vow of Ash | 1 Mana, 15 damage dan bayar 3 HP; tidak dapat dipakai pada HP <= 3. | Vow pertama memberi 3 Ward setelah pembayaran HP. |
| 12 | Gallows Bloom | 4 Mana, 11 damage; +11 terhadap target pada HP <= 30%. | Kill pertama yang dibuat Skill mana pun memulihkan 1 Mana. |
| 13 | Mirror Warden | 2 Mana, 8 damage dan 5 Ward untuk direct hit berikut. | Damage pertama yang diserap Ward memberi Basic Attack berikut +4. |
| 14 | Echo Script | 4 Mana, 9 damage dan setengah damage Basic Attack terakhir (maks. +5). | Biayanya -1 jika aksi sebelumnya Guard. |
| 15 | Abyssal Verdict | 5 Mana, 12 damage +4 per Burn yang dikonsumsi (maks. 3). | Jika mengonsumsi >=2 Burn dan target selamat, beri Curse 1 turn. |
| 16 | Fracture Lance | 2 Mana, 10 damage; +4 terhadap target Weakness. | Hit pertama pada target Weakness memberi 1 Mana. |
| 17 | Wraith Candle | 1 Mana, 5 damage, Burn 2 turn, pulihkan 2 HP. | Tick Burn pertama dalam battle memulihkan 2 HP tambahan. |
| 18 | Gloaming Bulwark | 3 Mana, tidak menyerang; 8 Ward untuk direct hit berikut. | Jika Ward menyerap damage, Guard berikut memblokir satu hit tambahan. |
| 19 | Requiem of Glass | 2 Mana, 8 damage dan cleanse satu Weakness/Curse. | Cleanse pertama yang berhasil memberi Weakness 1 turn ke musuh. |
| 20 | Orbital Sever | 3 Mana, 9 damage; +4 bila mengisi slot Orbit ketiga. | Convergence pertama setelah skill ini memulihkan 2 HP. |
| 21 | Asterion Gaze | 2 Mana, 6 damage; +4 sesudah Analyze. | Hit pertama dari intent yang diungkap Analyze dikurangi 3 damage. |
| 22 | Cinder Key | 3 Mana, 10 damage; konsumsi 1 Burn untuk +6 damage. | Konsumsi Burn pertama memberi 1 Ember. |
| 23 | Sable Suture | 2 Mana, pulihkan 7 HP dan 5 Ward. | Guard pertama setelah skill ini memulihkan 2 HP. |
| 24 | Echo Braid | 3 Mana, 7 damage +2 per jenis aksi berbeda di dua slot Orbit terakhir (maks. +4). | Basic Attack berikut mendapat +3 damage. |
| 25 | Ember Reprise | 2 Mana, 8 damage; +4 jika aksi sebelumnya Guard. | Skill pertama yang digunakan tepat setelah Guard memberi 1 Mana. |
| 26 | Nightfall Vow | 2 Mana, 18 damage dan bayar 5 HP; tidak dapat dipakai pada HP <= 5. | Penggunaan pertama memulihkan 2 Mana setelah membayar HP. |
| 27 | Gravity Thread | 2 Mana, 6 damage dan Weakness 2 turn (3 jika target sudah Weakness). | Weakness pertama dari skill ini ditambah 1 turn. |
| 28 | Paper Crown | 3 Mana, 8 damage; +6 terhadap target Cursed, atau beri Curse 2 turn. | Hit pertama terhadap target Cursed memperpanjang Curse 1 turn. |
| 29 | Mourning Bell | 3 Mana, 7 damage; +6 jika dua aksi Orbit sebelumnya memuat Attack dan Guard. | Convergence pertama dalam battle memberi 2 Mana. |
| 30 | Umbral Execution | 4 Mana, 11 damage; +9 saat target <=35% HP. | Hit pertama saat target berada di execute range memberi 4 Ward. |

### Aturan pemicu dan keseimbangan

- Passive hanya berjalan bila skill terkait terpasang pada snapshot loadout Run. Passive sekali-pakai ditandai hanya ketika syaratnya benar-benar terpenuhi; aksi yang gagal divalidasi tidak menghabiskan passive. Pengecualian yang tertulis jelas adalah diskon Echo Script, yang berulang setiap kali skill itu langsung didahului Guard.
- Rift Siphon memeriksa kematian musuh Cursed dari semua jalur damage dalam battle, termasuk serangan balik dan Burn tick. Gallows Bloom berbeda: syaratnya adalah skill pemain yang secara langsung mengalahkan musuh; kematian dari Basic Attack, Burn tick, parry, atau counter tidak dihitung sebagai Skill kill.
- Semua 30 identitas active dan passive dibedakan pada enum katalog dan deskripsi. Beberapa efek memang memakai trigger status yang sama (misalnya target Burning), tetapi hasil, skill pemilik, dan timingnya berbeda serta dibatasi sekali per battle.
- Damage bonus dihitung sebelum Critical. Heal, Mana, Ember, Ward, status, dan Convergence diproses hanya setelah skill berhasil membayar Mana. Status yang tersedia tetap Burn, Weakness, dan Curse.
- Tidak ada skill wajib untuk mengalahkan boss. Nilai belum dinyatakan final seimbang; tiga run otomatis menguji determinisme regresi, bukan pengganti playtest balance oleh pemain.

## 6. Data dan penyimpanan yang disarankan

### Katalog konten

- `game_seed.db` tetap menjadi sumber katalog resmi. Kode skill stabil (`SKL_*`) menjadi ID lintas versi; integer database tidak dipakai sebagai identitas save.
- Pertahankan tabel `skills` untuk nama, biaya, Power, target, deskripsi, dan icon. Tambahkan tabel seed `skill_effect_specs` untuk kode efek dan payload parameter aktif/passive yang tervalidasi. Ini menghindari perubahan bebas pada `SkillData` saat Content Manager internal membuat row demo.
- Katalog hanya menerima skill resmi yang memiliki row `skills` **dan** `skill_effect_specs`. Skill `SKL_CUSTOM_*` tanpa spesifikasi kurasi selalu ditolak.
- Database seed saat ini belum memiliki tabel versi schema. Tambahkan `schema_version` dan migrasi berurutan untuk seed/runtime database. Migrasi harus mempertahankan row custom internal, menambah definisi resmi dengan `INSERT OR IGNORE`/upsert terkontrol, serta tidak menyalin ulang seed di atas progres pemain.

### Data profil pemain

Buat `SkillProgressStore` offline di `Application.persistentDataPath/Database/player_progress.db`. Migrasikan schema melalui transaksi SQLite dan simpan satu profil lokal MVP (`profile_id = 1`). Tabel yang disarankan:

| Tabel | Data utama | Aturan kunci |
|---|---|---|
| `player_skill_unlocks` | `profile_id`, `skill_code`, `unlocked_utc`, `source_claim_id` | PK `(profile_id, skill_code)`; satu unlock per skill. |
| `player_skill_loadout` | `profile_id`, `slot_index`, `skill_code` | PK `(profile_id, slot_index)`; `UNIQUE(profile_id, skill_code)`; slot 0–2. |
| `skill_reward_claims` | `claim_id`, `run_id`, `milestone_code`, `offer_codes_json`, `chosen_skill_code`, `status`, timestamp | `UNIQUE(run_id, milestone_code)`; status `Pending` atau `Claimed`; offer tidak dapat diganti setelah disimpan. |

Penulisan reward dan unlock memakai satu SQLite transaction. Sebelum memilih, validasi bahwa skill belum dimiliki dan kodenya termasuk salah satu offer yang disimpan. Bila transaksi gagal, offer tetap Pending dan dapat dicoba lagi. History/Wallet yang sekarang berupa JSON tidak diubah oleh transaksi ini; RunId membuat setiap reward dapat dicocokkan tanpa klaim dua kali.

### Snapshot Run dan pemulihan

- Pada New Expedition, loadout tersimpan divalidasi terhadap koleksi serta katalog. Salin `EquippedSkillCodes` ke `SeededRunState` saat Run dibuat.
- Naikkan `SeededRunState` ke schema 4; validasi maksimal tiga kode berbeda dan semua kode harus tersedia di katalog resmi.
- Checkpoint schema 3 lama dimigrasikan menjadi loadout `[SKL_SHADOW_STRIKE]`. Jangan menghapus atau menolak save pemain lama hanya karena belum menyimpan pilihan loadout.
- Continue selalu memakai loadout snapshot dari checkpoint. Perubahan loadout profil tidak pernah mengganti skill di tengah Run aktif.
- Offer skill harus dicatat sebelum panel Summary dibuka agar tutup aplikasi, retry, atau buka Summary ulang tidak menghilangkan atau menggandakan hadiah.

## 7. Batas implementasi

### Sudah diimplementasikan

1. Migrasi katalog dan definisi efek skill resmi.
2. Store koleksi, loadout tiga slot, reward claim idempotent, dan migrasi save Run lama.
3. Skill Archive/Loadout yang dibuka dari Echo Archive.
4. HUD Battle dengan tiga tombol skill dan satu jalur validasi aksi.
5. Pilihan Skill Memory pada Standard Run terminal serta pondasi claim ID untuk checkpoint wave masa depan.
6. Roster 30 skill sebagai konten awal; semua nilai masih balance draft.

### Tetap di luar cakupan

- CRUD skill buatan pemain atau menampilkan Content Manager ke Main Menu.
- Menaikkan Base Stats, membuka slot keempat, membeli slot, gacha, atau pembelian uang nyata.
- Preset loadout bernama, mode Infinite/Legend runtime, online/cloud save, Ranked, skill crafting, upgrade permanen skill, AoE, dan art/VFX final.
- Mengubah reward Echo atau aturan relic selain perubahan yang diperlukan untuk menguji sinergi skill.

## 8. Kriteria penerimaan

1. First launch memiliki Shadow Strike di koleksi dan Slot 1; slot lain kosong. Run baru tetap dapat dimainkan.
2. Pemain dapat memasang hanya skill yang dimiliki, maksimal tiga, tanpa duplikasi; pilihan tetap ada setelah aplikasi dibuka ulang.
3. Skill dari `SKL_CUSTOM_*`, skill tidak dikenal, data rusak, atau kode yang tidak ada di katalog ditolak dengan pesan aman.
4. New Run menangkap snapshot loadout; Continue menggunakan snapshot lama meskipun loadout profil berubah.
5. Clear dan Defeat setelah minimal satu node menghasilkan satu offer. Abandon dan Run tanpa node tidak menghasilkannya.
6. Offer terdiri dari tiga skill belum dimiliki, deterministik dan tetap sama setelah restart. Bila tersisa satu atau dua, tidak ada duplikat. Offer Pending tidak hilang saat keluar dari Summary.
7. Pilihan grant tersimpan satu kali untuk setiap `RunId + milestone`; mengulang callback, klik ganda, recovery, atau membuka Summary kembali tidak memberi duplikat.
8. Skill sukses mengurangi Mana tepat sekali, mengakhiri giliran, memperbarui stat, dan mengisi satu Orbit slot. Gagal validasi tidak mengubah state.
9. Skill heal, defend, debuff, dan resource tetap masuk Orbit sebagai `Skill`. Analyze tidak masuk Orbit dan intent tersembunyi tidak bocor.
10. Passive skill yang tidak dipasang tidak berjalan; passive terpasang tidak dapat terpicu lebih dari sekali dalam battle yang sama.
11. Save schema 3 dapat dipulihkan sebagai Shadow Strike loadout dan skill reward tidak mengubah Base Stats.
12. UI lulus containment dan navigasi mouse/keyboard pada 1920×1080, 1280×720, dan 1280×1024.

## 9. Rencana dan status verifikasi

1. Ambil baseline Unity Play Mode sebelum perubahan passive; baseline tersimpan di report sesi sebelumnya.
2. Implementasi koleksi, loadout, reward, migrasi, UI, checkpoint dan 30-skill battle catalog selesai; lihat report sesi skill 14 November.
3. Audit silang 30 `ActiveEffect`, 30 `PassiveEffect`, passive text, event hook, serta syarat equipped/once-per-battle. Koreksi passive yang tidak sesuai tooltip; Echo Script tetap kondisional dan boleh berulang setiap kali Guard langsung mendahuluinya.
4. Tambahkan regression checks untuk identitas katalog, status-duration extension, Burn/Ember, Burn/Curse, dan sequencing Guard → Skill.
5. Jalankan suite Play Mode penuh **tiga kali berturut-turut**. Lulus hanya bila total sama, 0 failed dan 0 skipped; setiap pass punya profil, XML, dan log sendiri. Bila ada satu kegagalan, perbaiki penyebab dan ulangi semua tiga pass. **Selesai:** 168/168 lulus di ketiga profil terisolasi; 0 gagal dan 0 skip.
6. Perbarui report, konteks proyek, GDD, dan salinan context pack dengan desain maupun hasil aktual. **Selesai pada 15 November 2026.** Balance tetap provisional sampai owner playtest.

### Audit passive dan verifikasi — 15 November 2026

- Audit menemukan tooltip Cinder Key dan Abyssal Verdict tidak cocok dengan hook yang lama. Passive Cinder Key kini memberi Ember pada konsumsi Burn pertama; Abyssal Verdict kini memberi Curse hanya setelah mengonsumsi setidaknya dua Burn dan target tetap hidup.
- Menambahkan `BattleStatusController.ExtendDuration` untuk Gravity Thread dan Paper Crown dengan validasi durasi serta preservasi stack. Passive pemicu hanya mengonsumsi jatah saat skill terkait benar-benar terpasang.
- Memperbaiki cakupan pemicu: hit/kill yang memenuhi syarat dihitung lintas Basic Attack dan Skill; Gallows Bloom, Rift Siphon, Cinder Refrain, Fracture Lance, dan Paper Crown tidak lagi hanya aktif di jalur serangan yang salah.
- Menghapus efek lama yang bertentangan dengan desain pada Vow of Ash, Gloaming Bulwark, Requiem, Cinder Key, Nightfall Vow, Gravity Thread, Paper Crown, Mourning Bell, Abyssal Verdict, dan Umbral Execution.
- Tambahan Play Mode tests mengunci 30 active identity, 30 passive identity, deskripsi unique, duration extension, Cinder Key, Abyssal Verdict, Guard-conditioned passives, dan Rift Siphon dari Burn tick. Ketiga pass final masing-masing **168/168 PASS, 0 gagal, 0 skip**; profil, durasi, XML, log, dan source hash ada di [`Docs/TestResults/2026-11-15-SkillSystem/REPORT.md`](TestResults/2026-11-15-SkillSystem/REPORT.md).
- Test awal menemukan setup regresi memasang skill hanya di field HUD sehingga refresh mengembalikannya ke loadout Run; helper test telah diperbaiki agar memasang loadout ke `RunManager` seperti jalur produk. Hasil awal 165/167 itu hanya diagnostik, tidak dihitung sebagai pass final.

### Catatan implementasi 14 November 2026

- Runtime catalog memuat 30 skill resmi dan tiga slot tetap. Skill custom dari Content Manager tidak masuk ke roster pemain.
- SQLite profil lokal `player_progress.db` menyimpan koleksi, loadout, reward offer, dan klaim. Claim unik per RunId/milestone; offer disimpan sebelum ditampilkan dan dapat ditunda lalu dibuka kembali dari Skill Archive.
- Run baru membawa snapshot loadout. Checkpoint schema 3 dimigrasikan dengan Shadow Strike pada slot pertama.
- Skill Archive tersedia melalui Echo Archive di Main Menu. Skill Memory muncul setelah Clear/Defeat yang sudah menyelesaikan node.
- Nilai damage, Mana, dan passive masih provisional. Kelulusan tes tidak menggantikan playtest balance atau review visual.
- Hasil baseline dan pekerjaan pertama tiga pass Play Mode dicatat di `Docs/TestResults/2026-11-14-SkillSystem/REPORT.md`; regresi passive baru dilaporkan terpisah untuk 15 November.

## 10. Referensi

- [Chaos Zero Nightmare — developer note: Save Data dan Merit Battle](https://czn.gg/%E2%97%A4developer-notes-new-combatant-blazebell-new-great-rift-boss-and-more%E2%97%A2/) — sumber resmi; digunakan untuk prinsip pemisahan progress build dan fairness mode.
- [Chaos Zero Nightmare — How To Create Best Save Data (GameWith)](https://gamewith.net/chaoszeronightmare/72521) — panduan komunitas; menjelaskan pilihan apa yang dibawa ke Save Data dengan kapasitas terbatas. Nilainya patch-sensitive.
- [Slay the Spire 2 (Steam)](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) — sumber resmi; build berubah dari pilihan kartu/relic dan konten masih diatur selama Early Access.
- [Slay the Spire II (Wikipedia)](https://en.wikipedia.org/wiki/Slay_the_Spire_II) — ringkasan secondary untuk Timeline dan progres lintas percobaan.
- [Hades II — Grasp dan pemilihan Arcana (Shacknews)](https://www.shacknews.com/article/146102/upgrade-grasp-hades-2) — sumber sekunder untuk pola loadout sebelum Run yang dibatasi kapasitas.
- [Draf 15 skill SYNGRAVA](Skill_Design_Catalog_Draft.md) — sumber nilai awal internal, tetap provisional.
