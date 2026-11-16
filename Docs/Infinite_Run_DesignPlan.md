# Rancangan Infinite Run — SYNGRAVA

**Tanggal:** 16 November 2026
**Status:** Rancangan diperbarui dengan audit sumber primer; belum diimplementasikan. Gerbang Standard Run tetap wajib sebelum coding.
**Mode:** Infinite Casual, offline, satu pemain.
**Gerbang sebelum coding:** selesaikan dan tandatangani semua pemeriksaan Standard Run pada `StandardRun_ReleaseGate_Checklist.md` untuk executable kandidat yang akan menjadi dasar mode ini.

## 1. Ringkasan keputusan

Infinite Run adalah mode berulang yang meneruskan sistem node, combat, relic, anomaly, event, skill, checkpoint, dan Run Summary yang sudah ada. Satu **Cycle terdiri dari 10 node**; node ke-10 selalu boss. Setelah boss dikalahkan, pemain menerima hadiah checkpoint, peta Cycle berikutnya dibuat dari seed Run, lalu progres lanjut sampai Warden kalah atau pemain memilih mengakhiri Run pada checkpoint yang aman.

Mode ini memakai aturan, seed, score model, serta top-ten sendiri. Standard Run yang sekarang tidak berubah. Bonus stat tetap menjadi modifier sementara milik Run dan kembali ke baseline saat Run selesai. Skill Memory pada setiap boss memberi satu skill baru ke arsip permanen. Tidak ada perubahan Base Stats atau pembelian slot loadout.

## 2. Tujuan dan hal yang tidak dikerjakan

### Tujuan

- Membuat Run tanpa batas jumlah Cycle yang masih memiliki irama dan tujuan jangka pendek yang jelas.
- Memberi pemain waktu untuk membangun sinergi relic, anomaly, modifier, dan tiga skill.
- Menjaga reward skill permanen tetap deterministik, tidak dapat diduplikasi setelah crash atau klik berulang.
- Menyimpan Cycle, rute, checkpoint, score, dan history tanpa merusak save Standard lama.
- Membandingkan hanya hasil Infinite dengan Infinite pada leaderboard lokal.

### Di luar MVP

- Ranked, online/global leaderboard, season, daily seed, matchmaking, multiplayer.
- Boss, musuh, skill, relic, atau kelas karakter eksklusif Infinite.
- Reset/upgrade Base Stats, slot skill tambahan, gacha, atau reward yang membeli kekuatan permanen.
- Kesulitan tak terbatas secara numerik. Cycle tidak dibatasi oleh desain; angka runtime harus tetap aman dari overflow.

## 3. Hasil riset dan pelajaran desain

`Risk of Rain 2: Simulacrum` memberi pilihan tiga item setelah wave dan menaruh boss setiap lima wave. Video developer **Dev Thoughts #23** dan catatan patch resmi menggambarkan pola hadiah dan boss berkala; catatan patch juga menjelaskan bahwa wave dilewati setelah 60 detik tanpa spawn agar mode tidak macet. SYNGRAVA mengadaptasi pilihan reward, boss berkala, dan anti-soft-lock, tetapi tetap memakai satu boss tiap Cycle 10 node demi cocok dengan peta Standard yang ada. [Video Dev Thoughts #23](https://youtu.be/1EBxT0zwIVA), [catatan patch Simulacrum 1.2.2](https://store.steampowered.com/news/app/632360/view/3114807061149729826)

`Vampire Survivors` v1.0 mengulang gelombang tahap dalam Cycle Endless dan meningkatkan tekanan pada tiap pengulangan. SYNGRAVA mengambil bentuk pengulangan yang mudah dikenali, bukan angka scaling-nya: pertarungan turn-based dan modifier yang menumpuk perlu kurva HP/damage tersendiri serta pengujian kombinasi build. [Patch notes resmi Vampire Survivors v1.0](https://steamcommunity.com/games/1794680/announcements/detail/6186282400405707304)

**Penerapan sebagai inferensi desain:** SYNGRAVA memakai Cycle boss, pilihan Skill Memory, dan mutator yang dipilih dari katalog valid. Besaran scaling, bobot reward, dan batas performa perlu diukur pada prototipe; sumber di atas bukan bukti bahwa angka permainan mereka cocok untuk SYNGRAVA.

### Audit riset lanjutan — 16 November 2026

Sumber primer Hopoo Games memperjelas pola Simulacrum: Endless dimainkan dalam wave dengan boss berkala, mutator pada sebagian wave, dan tiga pilihan reward sesudah wave. Catatan patch resmi kemudian menambahkan batas kredit spawn dan melewati wave bila tidak ada monster yang muncul selama 60 detik; developer menyebut edge case tersebut dapat membuat mode berhenti maju. Untuk SYNGRAVA, yang diambil adalah milestone yang mudah dibaca, reward pada batas aman, variasi terkurasi, dan jaminan state selalu bisa maju atau pulih. Timer 60 detik dan cadence boss lima wave tidak dipakai karena combat SYNGRAVA berbasis giliran dan Cycle-nya sudah ditetapkan 10 node. [Hopoo Games, Dev Thoughts #23](https://store.steampowered.com/news/posts/?appids=632360&enddate=1650380344&feed=steam_community_announcements) · [Hopoo Games, Simulacrum patch 1.2.2](https://store.steampowered.com/news/app/632360/view/3114807061149729826)

Pemeriksaan ulang halaman resmi Vampire Survivors tidak mengembalikan isi patch 1.0 yang bisa dibaca melalui browser riset. Karena itu, gagasan stage yang berulang tetap dicatat sebagai referensi konseptual dari riset sebelumnya, tetapi tidak dipakai untuk menetapkan angka scaling atau aturan baru. Slay the Spire 2 juga masih berstatus Early Access pada halaman resminya; perubahan konten dan balance di sana tidak dijadikan spesifikasi SYNGRAVA. [Slay the Spire 2 — halaman resmi Steam](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/)

Konsekuensi desain yang dikunci untuk prototipe:

- Setiap node dan boss clear harus berakhir pada state yang tersimpan dan dapat dilanjutkan; generator tidak boleh menghasilkan Cycle tanpa boss yang dapat dicapai atau reward tanpa pilihan valid.
- Jika encounter, offer, atau transisi Cycle tidak valid, Run mempertahankan checkpoint valid sebelumnya dan masuk ke jalur Recovery yang menjelaskan masalah. Jangan mengganti data committed secara diam-diam atau menunggu tanpa batas.
- Kesulitan naik lewat ruleset Infinite berversi dan diuji terhadap build/loadout, bukan dengan menyalin persentase dari game real-time. Nilai scaling tetap parameter prototipe sampai simulasi menunjukkan kisaran yang aman.
- Riset ini tidak mengubah keputusan inti: 10 node per Cycle, boss di node 10, reward Skill Memory setelah boss, dan modifier sementara yang hilang saat Run berakhir.

## 4. Alur pemain

1. Di Main Menu, pemain memilih **Infinite Casual**. Entri hanya aktif setelah gerbang Standard Run pada Bagian 13 lulus.
2. Briefing menjelaskan mode, carry-over, siklus 10 node, checkpoint, score, dan risiko Run berakhir saat kalah. Continue tersedia bila ada checkpoint Infinite yang valid.
3. Pemain memilih tiga skill dari koleksi yang sudah dimiliki; snapshot loadout dikunci untuk Run tersebut.
4. Run dimulai dengan seed dan ruleset Infinite yang tercatat. Setiap Cycle memiliki node 1–9 dari aturan Standard yang disesuaikan, dan node 10 adalah boss.
5. Node diselesaikan dengan sistem pilihan/reward yang sudah ada. Setelah reward node disimpan dan diterapkan, checkpoint aman dibuat.
6. Mengalahkan boss menutup Cycle, menyimpan hasilnya, lalu membuka **Skill Memory**: tiga skill resmi yang belum dimiliki; pemain memilih satu untuk koleksi permanen. Setelah claim commit, Cycle selanjutnya dimulai.
7. Jika katalog skill sudah lengkap, Skill Memory diganti oleh pilihan satu modifier sementara dari tiga kandidat valid. Tidak ditampilkan skill duplikat atau pilihan kosong.
8. Saat kalah, Summary menunjukkan score, Cycle terakhir, node, boss, build, rute, dan hasil reward yang telah committed. **Retire at Checkpoint** boleh mengakhiri Run dengan hasil `Retired`; Abandon dari menu tetap menandai `Abandoned` dan tidak masuk peringkat.

Continue memulihkan mode, seed, Cycle, node, checkpoint combat, route ledger, modifier, build, pending offer, dan snapshot skill Run yang sama. Mengubah loadout di Archive hanya berlaku pada Run baru.

## 5. Struktur Cycle, node, dan encounter

| Posisi di Cycle | Fungsi rancangan |
|---:|---|
| 1–3 | Pembuka: battle, event, relic, atau rest yang memenuhi kuota map. Tidak ada boss. |
| 4–6 | Build tumbuh; elite mulai masuk dalam pool yang memenuhi batas Standard. |
| 7–9 | Ancaman tinggi dan kesempatan recovery yang terbatas; node harus tetap punya pilihan yang valid. |
| 10 | Boss wajib. Tidak dapat dilewati dengan memilih node lain. Clear boss mengunci Cycle dan memulai Skill Memory. |

Peta setiap Cycle harus memiliki graf node terhubung dan boss yang selalu dapat dicapai. Aturan rute dapat berubah per Cycle, tetapi kuota encounter dan lokasi rest/boss mengikuti validator. Tidak membuat delapan atau sepuluh node palsu untuk sekadar memperpanjang Run.

Untuk konsistensi dan reproducibility, seed map dihitung dengan fungsi hash stabil dari `(RunSeed, CycleIndex, InfiniteRulesetVersion, "MAP")`; seed offer memakai domain label berbeda. Jangan memakai `string.GetHashCode()` yang bisa berubah lintas proses/runtime. Generator harus menolak atau memperbaiki peta yang tidak memenuhi kuota tanpa mengubah seed map yang sudah committed.

## 6. Kurva kesulitan dan reward

Kesulitan harus bertambah per Cycle tetapi tetap memperlihatkan ruang untuk membangun counter. Semua parameter berada pada data aturan berversi, bukan konstanta yang tersebar di UI/combat.

| Elemen | Proposal untuk prototipe | Guard keselamatan |
|---|---|---|
| HP musuh | Pertumbuhan bertahap per Cycle, dengan titik perubahan pada Cycle tertentu; kalkulasi fixed-point dari nilai dasar. | Clamp ke batas model dan tolak non-finite/negatif sebelum combat. |
| Damage musuh | Naik lebih lambat dari HP agar kekalahan tidak berubah menjadi one-shot mendadak. | Encounter mengumumkan modifier sebelum pilihan aksi; damage disaturasi sebelum `int` runtime. |
| Elite/boss | Pool musuh dan aturan ability bertambah menurut eligibility depth. Boss tetap menjadi lonjakan kesulitan yang terbaca. | Setiap boss rule harus dapat menghasilkan aksi valid dan lolos batas giliran. |
| Reward tier | Tier makin eligible; pemilihan memakai bobot yang terlihat/tercatat. | Eligibility tidak berarti guaranteed rarity tertinggi. Seeded choices disimpan sebelum UI. |
| Anomaly/mutator | Maksimal satu anomaly baru per Cycle pada awal MVP; mutator konflik dikeluarkan oleh validator. | Tampilkan perubahan gameplay di briefing sebelum memasuki node. |
| Modifier sementara | Boleh menumpuk tanpa batas cap gameplay yang arbitrer. | Hitung menggunakan `long`/fixed-point dengan saturating math dan laporkan nilai efektif konsisten. Tidak pernah menulis ke Base Stats. |

Nilai persentase belum dipatok sebelum simulasi; jangan mengasumsikan kenaikan linear besar seperti mode real-time cocok untuk turn-based. Tuning dilakukan lewat batch deterministik pada seed, variasi loadout, dan build rendah/tinggi: hitung jumlah giliran, damage masuk/keluar, win rate simulasi, dan Cycle ketika satu encounter melewati batas waktu uji.

## 7. Skill Memory dan klaim reward

- Satu boss clear menghasilkan satu milestone unik `INFINITE_CYCLE_<n>` pada `RunId` yang sama.
- Buat 3 kode berbeda dari skill resmi yang belum dimiliki berdasarkan seed + Cycle + versi katalog; simpan offer sebagai `Pending` **sebelum** panel ditampilkan.
- Pemain memilih satu. Unlock permanen dan receipt milestone ditulis secara atomik. Callback berulang mengembalikan hasil claim lama dan tidak memberi skill kedua.
- Offer pending setelah crash/relaunch harus muncul persis sama di Recovery.
- Skill yang dibuka langsung masuk Archive profil dan tersedia untuk Run berikutnya; loadout Run aktif tetap snapshot awal.
- Setelah koleksi penuh, kandidat reward beralih ke modifier sementara; tidak ada Echo conversion implisit. Kebijakan ekonomi ini harus tampak di panel dan masuk ruleset.
- Skill Memory dari boss yang sudah committed tidak dibatalkan saat pemain Retire atau mengalami Defeat di Cycle selanjutnya. Boss clear yang belum commit tidak boleh memajukan Cycle.

Store saat ini punya offer Standard unik per `(RunId, milestone)`, transaksi claim, dan pending offer. Generalisasi harus menguji jalur Infinite baru secara terpisah dan menyimpan `Mode`, `Cycle`, dan `RulesetVersion`; jangan mengganti ID Standard yang lama.

## 8. Penyimpanan, kompatibilitas, dan pemulihan

### Batas model sekarang

`RunManager.NodeMap`, `SeededRunState`, dan `ScoreCalculator` saat ini mewakili satu Standard map 8–10 node. History/Echo merangkum `VisitedNodeCodes` dari satu map. Model ini tidak boleh dipaksa menyimpan semua Cycle Infinite sebagai satu Standard map: validator Standard, score cap, award rute, dan migrasi checkpoint lama akan rusak.

### Perubahan terisolasi yang direkomendasikan

- Tambahkan `RunMode` eksplisit (`STANDARD`, `INFINITE_CASUAL`) pada checkpoint, terminal record, Skill Memory receipt, dan leaderboard. Mode kosong pada History lama dimigrasikan sebagai Standard; jangan menebak data yang rusak.
- Tambahkan state Infinite versioned, misalnya `InfiniteRunState`, yang menunjuk `RunId`, `RunSeed`, `RulesetVersion`, `CycleIndex`, `DepthInCycle`, seed/map current cycle, snapshot loadout, current battle/node, pending reward, dan status Run.
- Simpan ringkasan rute setiap Cycle yang sudah ditutup sebagai record immutable (node code/type/score inputs/boss clear); checkpoint aktif hanya butuh ledger committed + state Cycle sekarang. Jangan regenerate rute yang sudah diselesaikan dari ruleset terbaru.
- Map satu Cycle boleh memakai validator baru dengan tepat 10 node; jangan longgarkan `StandardRunNodeMap` global tanpa kebutuhan karena Standard dan test lama bergantung pada batasnya.
- Naikkan schema dengan migrasi bertahap. Semua checkpoint Standard lama tetap di schema 4 atau dimigrasikan eksplisit tanpa memberi mode Infinite. Backup terakhir tetap dipertahankan sebelum replace atomik.
- Checkpoint setelah node selesai, reward sudah diterapkan, dan state siap untuk pilihan aman. Jangan menyimpan state di tengah transaksi musuh atau setelah skill dibayar sebelum efeknya diterapkan.
- Boss commit harus atomic secara logis: terminal combat state, Cycle result, Skill Memory offer, reward receipt, lalu safe boundary berikutnya. Tiap tahap harus idempotent sehingga recovery dapat menyelesaikan transaksi setengah jadi tanpa menduplikasi reward.

### Rute, Echo, dan Summary

Archive rute seluruh Cycle untuk score/Run History. Untuk menjaga ukuran data, tiap Cycle menyimpan ringkasan kompak, bukan salinan state combat. Summary menampilkan total node, Cycle maksimum/selesai, boss defeated, score per kategori, durasi, relic/anomaly/modifier final, skill loadout awal, dan ringkasan route. Echo settlement memakai receipt per Run, satu kali, dengan saturating `long` sebelum konversi ke saldo; jangan membayar setiap restore.

## 9. Score dan Leaderboard

- Buat score model Infinite berversi dan pisahkan dari `ScoreCalculator` Standard v2. Score merefleksikan node valid diselesaikan, event/elite/boss, convergence scoreable, dan Cycle; durasi dan Ember yang bisa difarm tidak jadi poin.
- Semua input score berasal dari ledger Cycle committed, bukan counter UI atau field score yang bisa diedit. Validasi `RunId`, seed, ruleset, Cycle, dan hasil boss.
- Gunakan `long` pada akumulasi; tampilkan suffix/format ringkas pada UI bila melewati batas label. Urutan untuk hasil identik: score menurun, Cycle menurun, node dalam Cycle menurun, boss count menurun, durasi menaik, tanggal UTC menaik, `RunId` ordinal.
- Top 10 Infinite terpisah dari Standard Top Runs. Legacy History tanpa `Mode` tidak masuk Infinite. Retired score boleh masuk hanya bila diakhiri dari safe checkpoint dan seluruh state valid; Abandoned tidak masuk ranking.
- Score final dan History hanya ditulis sekali saat `Defeated` atau `Retired`. Pemain tidak dapat mengklaim score sementara dan melanjutkan state yang sama setelah terminal commit.

## 10. UI/UX

1. Main Menu: Infinite Casual jelas berbeda dari New Expedition, menampilkan eligibility/gate dan tombol Continue Infinite bila ada save valid.
2. Briefing: “10 nodes per Cycle · Boss di node 10 · build sementara hilang saat Run berakhir · Skill Memory memberi skill permanen.”
3. Peta: penanda `CYCLE 01`, `NODE 03/10`, indikator boss di node terakhir, dan ringkasan perubahan anomaly/mutator.
4. HUD dan battle log menampilkan Cycle/node; status skill/passive menjelaskan efek pemicu yang aktif.
5. Setelah boss: panel Skill Memory menampilkan tiga nama, active, passive, mana cost, dan cara membuka. Claim state memblokir klik ganda serta memiliki tombol Continue setelah saved receipt.
6. Continue/Recovery: label mode dan Cycle jelas agar pemain tidak salah membuka save. Corrupt checkpoint mengarah ke Recovery/backup, bukan membuat Run palsu.
7. Terminal Summary: badge Defeated/Retired, Cycle, nodes, boss, score/model/ruleset, build, route per Cycle dan CTA ke Menu, Run History, Top Runs.
8. Leaderboard: tab Standard / Infinite; tiap row memperlihatkan Cycle dan depth, serta ruleset. Focus keyboard/controller dan navigasi resolusi diuji bersamaan.

## 11. Risiko dan mitigasi

| Risiko | Pencegahan dan bukti yang dibutuhkan |
|---|---|
| Rute panjang menumbuhkan save tanpa batas | Simpan ledger ringkas per Cycle, batas ukuran snapshot, dan uji serialisasi pada 100 Cycle. Tidak membuang score inputs yang dibutuhkan untuk re-score. |
| Overflow stat/damage/score/Ember | `long`, checked/saturating helper terpusat, validasi rentang, uji Cycle sangat besar dan nilai maksimum. |
| Infinite map dapat buntu atau tanpa boss | Validator graf per Cycle, deterministic fallback dengan derivasi seed, tes properti untuk ribuan seed. |
| Soft-lock karena wave/aksi boss/reward tidak tersedia | Timeout/fallback encounter berbasis batas, pilihan reward selalu 1–3, smoke Continue saat safe checkpoint; pelajaran dari patch Simulacrum. |
| Duplikasi Skill Memory setelah crash | Milestone unik, offer tersimpan sebelum UI, transaction claim, recovery di tiap titik interupsi. |
| Balance runaway atau build tidak lagi relevan | Simulasi banyak kombinasi loadout, eligibility reward berbobot, kurva damage lebih lambat, review metrik dan playtest pemilik. |
| Standard Run lama ikut berubah | `RunMode` eksplisit, calculator/map terpisah, regresi Standard penuh, migrasi dengan fixture checkpoint/history lama. |
| Abandon/Retire bisa dieksploitasi | Hanya safe checkpoint yang boleh Retire; tiap klaim/cycle hanya sekali; Abandon tak ranked; test retry/restore. |

## 12. Kriteria penerimaan dan verifikasi sebelum rilis

- Infinite terkunci sebelum gerbang Standard Run selesai; Standard New Expedition, Continue, score, History, dan leaderboard tidak berubah.
- Untuk seed sama + ruleset sama, map/offer/stat progression sama di mesin/process terpisah.
- Setiap Cycle memiliki 10 node, jalur valid, boss hanya di node 10, dan boss clear memulai Cycle berikutnya tepat satu kali.
- Save di boundary node, tengah battle bila didukung, Skill Memory pending, setelah claim, dan setelah boss memulihkan state/offer yang sama.
- Defeat/Retire merekam satu terminal row; Abandon tidak ranked; retry/relaunch tidak menggandakan history, Echo, skill, atau modifier.
- Skill reward menyediakan 3 skill tidak dimiliki unik; complete collection memakai fallback modifier yang berbeda dan terdokumentasi.
- Leaderboard Infinite memfilter mode, ruleset/score model yang kompatibel, RunId duplikat, Retired dan Abandoned; hasil Standard tetap terpisah.
- Tidak ada overflow, rute buntu, peta boss tanpa reward, offer kosong, soft-lock, atau panel yang tidak bisa dipakai keyboard/controller.
- Build Windows lulus smoke visual dan automated Play Mode **tiga kali berturut-turut** dengan profil terisolasi, log dan XML terpisah; setiap hasil lulus harus 0 failed dan 0 skipped. Perbaikan apa pun memulai ulang rangkaian tiga pass.
- Tuning balance mendapat playtest pemilik; test otomatis tidak diklaim sebagai pengganti acceptance gameplay manual.

## 13. Gerbang sebelum mulai implementasi

Rancangan ini selesai, namun implementasi belum dimulai. Sebelum coding, ikuti checklist `StandardRun_ReleaseGate_Checklist.md`: pastikan Continue/cold resume, Recovery, Defeat, Abandon/Cancel, save retry, Clear dan transaksi History/Top Runs lolos pada executable kandidat. Catatan proyek menyebut gerbang ini masih memiliki pemeriksaan manual yang belum terbukti untuk build kandidat terkini. Jika blocker ditemukan, perbaiki Standard Run terlebih dahulu; jangan membuat mode Infinite menggandakan bug checkpoint atau score.

**Pemeriksaan status 16 November 2026:** pemilik sudah menerima tiga Standard Run Clear 10/10 pada kandidat yang dicatat di checklist dan mengirim dua Clear tambahan. Namun Continue setelah cold relaunch, Recovery, Defeat, Abandon, dan retry saat save gagal belum dibuktikan semuanya pada kandidat yang sama. Status gate tetap **belum lengkap** sampai tiap transaksi itu memiliki bukti automated atau owner-accepted yang terikat pada build/profile tepat; tiga clear saja tidak membuktikan jalur pemulihan tersebut. Ini prasyarat rilis fitur, bukan pekerjaan implementasi Infinite yang sudah berjalan.

Setelah gate lulus, implementasi bertahap: (1) model/ruleset/validator map per Cycle; (2) state/save/history migration; (3) cycle rewards dan Skill Memory receipts; (4) scaling/score; (5) UI mode/recovery/leaderboard; (6) automated property, migration, transaction, end-to-end tests; (7) tiga full Play Mode pass terisolasi dan Windows smoke; (8) owner playtest dan balance approval.

## Referensi

- [Risk of Rain 2 / Hopoo Games — Dev Thoughts #23](https://youtu.be/1EBxT0zwIVA) — video developer resmi tentang Simulacrum.
- [Risk of Rain 2 / Hopoo Games — patch Simulacrum 1.2.2](https://store.steampowered.com/news/app/632360/view/3114807061149729826) — catatan resmi untuk pilihan 3 item, boss setiap 5 wave, dan fail-safe wave tanpa spawn.
- [Vampire Survivors / poncle — v1.0 patch notes, Endless Mode](https://steamcommunity.com/games/1794680/announcements/detail/6186282400405707304) — sumber resmi untuk pengulangan wave sebagai Cycle dan scaling antar-cycle; angka mereka tidak diadopsi.
- [`PROJECT_CONTEXT.md`](SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md) — ketentuan mode Infinite SYNGRAVA, stat sementara, gerbang, dan leaderboard.
- [`StandardRun_ReleaseGate_Checklist.md`](StandardRun_ReleaseGate_Checklist.md) — pemeriksaan produk yang harus lulus sebelum mode Infinite dikodekan.
- [`Skill_Collection_Loadout_DesignPlan.md`](Skill_Collection_Loadout_DesignPlan.md) — Skill Archive, snapshot loadout, Skill Memory, dan daftar roster kurasi.
