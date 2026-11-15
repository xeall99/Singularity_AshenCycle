# Local Leaderboard — 25 September 2026

## Hasil yang diselesaikan

Main Menu sekarang menyediakan **TOP RUNS**, yang membuka **FATE LEDGER**: sepuluh hasil Standard Run dengan skor tertinggi dari Run History lokal. Memilih baris memperlihatkan seed, tanggal, ruleset, dan rincian poin. Panel menggunakan hitam/violet/putih SYNGRAVA dengan aksen merah untuk Defeated. Rencana data dan layout ditulis sebelum implementasi di `Docs/LocalLeaderboard_DesignPlan.md`.

Leaderboard membaca arsip yang sama dengan Run History, tanpa file hasil kedua atau transaksi tambahan. Hasil Cleared maupun Defeated dapat masuk selama menggunakan model skor saat ini. Riwayat sebelum fitur skor tetap tersedia di Run History sebagai UNSCORED dan tidak ikut peringkat. Riwayat Standard lama yang sudah mempunyai skor tetapi belum mempunyai mode/ruleset/cycle tetap diterima tanpa mengubah skornya.

Urutan: skor tertinggi → node terselesaikan terbanyak → boss terbanyak → durasi terpendek → tanggal selesai paling awal → RunId ordinal. Aturan terakhir menjaga urutan stabil saat seluruh nilai sebelumnya sama. Standard Run selalu cycle 1; Infinite dan peringkat online belum diimplementasikan.

## Perubahan kode

| File | Perubahan |
| --- | --- |
| `Assets/Project/Scripts/Battle/LocalLeaderboardService.cs` + `.meta` | Proyeksi read-only, filter mode/model, penolakan identitas ganda, tie-break, top ten, dan salinan entry yang tidak berubah ketika DTO asal dimutasi. |
| `Assets/Project/Scripts/Battle/RunHistoryStore.cs` | Metadata optional mode/ruleset/cycle; kompatibilitas field mode lama yang hilang atau kosong. |
| `Assets/Project/Scripts/UI/LocalLeaderboardUI.cs` + `.meta` | Panel 10 baris dengan kolom terpisah, strip detail, empty/error states, navigasi fokus yang tetap di dalam panel, serta Cancel/Close kembali ke pembuka. |
| `Assets/Project/Scripts/UI/MainMenuUI.cs` | Tombol TOP RUNS, footer dipindah ke bawah, bootstrap leaderboard, warna keyboard selection tetap gelap dan terbaca. |
| `Assets/Tests/PlayMode/BattleSnapshotTests.cs` | Tiga kasus leaderboard dan pengujian layout tombol Main Menu yang baru. |

ScoreCalculator dan integrasi Run Summary/Run History sudah diselesaikan pada milestone tepat sebelumnya. Bukti dua run 127/127 ada di `Docs/TestResults/2026-09-25-ScoreCalculator/REPORT.md`; regresi final di sini juga menjalankan kembali semua kasus tersebut.

## Pengujian Unity aktual

Unity **6000.6.0f1**, PlayMode `BattleSnapshotTests`, dijalankan dengan `-batchmode -runTests -testPlatform PlayMode -testFilter BattleSnapshotTests` di salinan proyek terisolasi. Company/product test terpisah dari proyek pengguna sehingga runtime database, checkpoint, Echo, dan riwayat pengguna tidak dipakai sebagai fixture.

| Run | Hasil | Durasi suite |
| --- | --- | --- |
| Pertama, `first-results.xml` | 128/129 lulus; 1 gagal; 0 skipped | 70.6586925 detik |
| Final sesudah perbaikan, `final-results.xml` | **130/130 lulus; 0 gagal; 0 skipped** | **70.4795756 detik** |

Kegagalan pertama nyata dan telah diperbaiki: `JsonUtility` menulis kembali field string mode yang belum ada pada arsip lama sebagai string kosong. Validator awal hanya menerima null sebagai mode legacy; akibatnya primary yang baru ditulis ditolak dan pembacaan kembali ke backup yang tidak memuat commit terbaru. Validator dan filter leaderboard kini menerima null maupun kosong untuk arsip Standard lama. `TC_LDB_003` membuat arsip tanpa metadata tersebut, menambahkan dua hasil baru, membuka ulang store, dan membuktikan ketiga hasil serta skor lama tetap ada.

Kasus lainnya membuktikan top-ten/tie-break, identitas tidak muncul dua kali, mode lain dan hasil unscored dikecualikan, entry hasil terlepas dari DTO asal, serta menu dapat membuka dan menutup panel. UI diuji untuk pemilihan baris melalui fokus, navigasi ke Close, Cancel yang mengembalikan fokus, daftar kosong, dan file rusak. Primary/backup dibandingkan byte-per-byte sebelum dan setelah panel dilihat: tetap sama.

Kompilasi Unity: **0 error C#**. Masih ada empat warning CS0618 yang sudah ada untuk `FindFirstObjectByType`; rinciannya disimpan pada `compilation-warnings.txt`. Warning ini bukan kegagalan tes.

## Pemeriksaan visual dan kesesuaian source

- Main Menu dan leaderboard dirender pada **1920×1080** dan **1280×1024**. Keempat PNG final diperiksa secara visual; kolom terpisah, teks berada di dalam panel, tombol TOP RUNS tidak bertabrakan dengan footer, dan baris terpilih memakai violet gelap dengan teks terang.
- Assertion TextMesh Pro overflow dan containment dijalankan pada kedua resolusi leaderboard. Main Menu ikut regression layout yang sudah tersedia.
- Screenshot leaderboard menggunakan hasil sintetis untuk tes; durasi 00:00 pada gambar adalah nilai fixture, bukan hasil playthrough manual.
- `source-hashes.csv`: **11/11** script sumber/tes workspace sama dengan salinan Unity yang menghasilkan XML final, termasuk ScoreCalculator dan integrasinya.
- `git diff --check` pada perubahan tracked yang terkait lulus.

## Penggunaan, batas, dan langkah berikutnya

Buka `Assets/Project/Scenes/MainMenu.unity`, Play, lalu pilih **TOP RUNS**. Selesaikan Standard Run untuk menambah hasil baru. Panel akan menampilkan keadaan kosong sampai ada hasil dengan skor. Tidak ada Inspector assignment, scene/prefab migration, paket, aset gambar baru, atau perubahan SQLite yang diperlukan.

Belum dilakukan Windows standalone build maupun tiga playthrough Standard Run manual berturut-turut. Balance skor masih baseline GDD; Local Leaderboard bukan sistem Ranked/anti-cheat/cloud. Figma/Canva tidak menghasilkan desain baru pada kelanjutan ini; layout mengikuti rencana tertulis dan palet/komponen proyek, lalu dibuktikan dengan render Unity.

Langkah terkecil berikutnya: verifikasi Standard Run dari Main Menu sampai Clear/Defeat/Abandon, Continue/Recovery, reset temporary build, dan pencatatan hasil pada Windows build. Gerbang tiga Standard Run berturut-turut tetap harus dibuktikan sebelum membuka milestone Infinite Casual. Tidak ada commit atau push dalam pekerjaan ini.
