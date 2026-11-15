# Laporan audit keterbacaan frekuensi passive

**Tanggal:** 15 November 2026
**Unity:** 6000.6.0f1
**Status:** tiga pass Play Mode berturut-turut lulus; tidak ada perubahan perilaku combat.

## Tujuan dan lingkup

Audit ini menutup celah pada teks Skill Archive dan kartu Skill Memory: pemain harus dapat melihat apakah passive hanya terpicu sekali setiap battle atau dapat berulang. Katalog berisi 30 skill. Dua puluh sembilan passive one-shot kini menyebut `each battle` atau `once per battle`; Echo Script satu-satunya yang berulang dan menandai dirinya `repeatable` pada teks pendek maupun panjang.

`SkillCatalog.cs` hanya mengubah `Description` dan `PassiveDescription`. Tidak ada perubahan pada hook runtime, angka damage/Mana, ataupun aturan battle. Tes katalog memverifikasi 30 active effect yang unik, 30 passive effect yang unik, 30 deskripsi passive yang unik, 29 label one-shot, dan satu label repeatable.

## Hasil

Baseline dijalankan sebelum menyalin perubahan source ke salinan Unity tes. Tiga pass final memakai company name yang berbeda agar `Application.persistentDataPath` dan save tes terisolasi tiap pass.

| Tahap | Profil `companyName` | Hasil | Lulus | Gagal | Skip | Durasi suite |
|---|---|---:|---:|---:|---:|---:|
| Baseline | `syngrava-battle-tests-e57ef05c80124e46b37eba1b25fe3c33` | Passed | 168 | 0 | 0 | 166.2178714 dtk |
| Final pass 1 | `SYNGRAVA_SkillFrequency_FinalPass1_20261115` | Passed | 168 | 0 | 0 | 178.9865089 dtk |
| Final pass 2 | `SYNGRAVA_SkillFrequency_FinalPass2_20261115` | Passed | 168 | 0 | 0 | 151.0279017 dtk |
| Final pass 3 | `SYNGRAVA_SkillFrequency_FinalPass3_20261115` | Passed | 168 | 0 | 0 | 158.2916065 dtk |

Semua final pass menggunakan suite penuh `BattleSnapshotTests`, bukan hanya pemeriksaan katalog. XML melaporkan `Passed`; log tidak menunjukkan error kompilasi C#. Unity batch menulis warning lisensi/Direct3D lama, tetapi setiap suite selesai dengan 0 failed dan 0 skipped.

### Koreksi diagnostik

Percobaan kandidat pertama menghasilkan 167/168 karena assertion hanya menerima frasa `each battle`. Teks Vesper Stitch sudah menyatakan batas yang sama dengan frasa `Once per battle`, sehingga assertion terlalu sempit. Test diperbaiki untuk menerima kedua frasa yang jelas; teks skill tidak perlu diubah lagi. Percobaan diagnostik 167/168 **tidak dihitung** sebagai pass acceptance dan XML/log-nya tetap disimpan.

### Isolasi dan integritas source

Unity menjalankan tes di salinan sementara proyek, bukan proyek kerja atau save pemain. Satu clone yang sudah terimpor dipakai untuk baseline dan final suite; `companyName` diganti sebelum setiap pass sehingga direktori save tes unik. XML dan log setiap tahap disimpan terpisah di folder ini. SHA-256 katalog dan tes pada clone cocok dengan workspace (**2/2 source cocok**); rinciannya ada di [`source-hashes.csv`](source-hashes.csv).

Artefak:

- `baseline-results.xml`, `baseline-unity.log`
- `final-pass-1-results.xml`, `final-pass-1-unity.log`
- `final-pass-2-results.xml`, `final-pass-2-unity.log`
- `final-pass-3-results.xml`, `final-pass-3-unity.log`
- `diagnostic-pass1-failed.xml`, `diagnostic-pass1-failed.log` — diagnostik assertion sebelum koreksi
- `source-hashes.csv`

## Riset skill dan batas klaim

Rujukan tier tertinggi yang dapat dibaca menunjukkan pola seperti passive takedown yang bertumbuh pada unit Secret Anime Expeditions, dan kit tier tinggi yang menggabungkan crowd control, summon, atau buff tim. Artikel komunitas Anime Crusaders mengonfirmasi Mythic/Secret berevolusi dapat memperoleh passive baru. Wiki asli yang diberikan pemilik tidak menyediakan detail yang bisa diverifikasi saat audit: beberapa menolak akses HTTP 403 dan halaman `wiki.32x.dev` tidak memuat isi ability. SYNGRAVA menggunakan prinsip trigger dan payoff yang jelas, lalu mengadaptasikannya ke aturan turn-based sendiri; nama, karakter, angka, teks, dan urutan ability tidak disalin.

- [Anime Expeditions — Secret, Exclusive, Mythic, dan Legendary passive/active (All Things How)](https://allthings.how/anime-expeditions-every-unit-passive-and-active-ability-by-rarity/)
- [Anime Expeditions — daftar passive/active (Pro Game Guides)](https://progameguides.com/roblox/all-anime-expeditions-unit-passives-active-abilities/)
- [Anime Crusaders — unit dan passive evolusi (BloxInformer)](https://bloxinformer.com/wikis/anime-crusaders/units/)

Tes ini memverifikasi kontrak data dan regresi otomatis, bukan keseimbangan damage, kualitas keterbacaan visual pada semua ukuran layar, atau hasil belajar pemain. Angka skill tetap provisional sampai playtest owner. Rancangan fitur berikutnya, Infinite Casual, sudah ditulis dan diteliti di [`Docs/Infinite_Run_DesignPlan.md`](../../Infinite_Run_DesignPlan.md); implementasi tetap menunggu release gate Standard Run.
