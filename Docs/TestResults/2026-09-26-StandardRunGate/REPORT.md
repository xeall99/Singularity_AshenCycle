# Standard Run integration and Windows release gate — 26 September 2026

## Scope

Milestone setelah Main Menu adalah membuktikan integrasi Main Menu → Standard Run → terminal summary/history/leaderboard, transaksi Continue dan New Expedition, lalu membuat build Windows. Pekerjaan ini tidak menambahkan mode Infinite dan tidak mengubah aturan combat atau hadiah.

## Verifikasi otomatis

Tes dijalankan dengan Unity `6000.6.0f1` pada salinan proyek terisolasi dengan identitas produk `SYNGRAVA_BattleTests`. Identitas terpisah ini mencegah tes checkpoint/history/wallet menyentuh profil pemain di proyek utama. `source-hashes.csv` menunjukkan scene, sumber runtime, tes, dan Build Settings yang diuji identik dengan workspace dan salinan build pada saat verifikasi.

| Suite | Hasil | Cakupan tambahan |
| --- | --- | --- |
| Fokus `TC_E2E_` | **2/2 PASS**, 0 gagal, 0 skip, 5.286 detik | Clear lintas Main Menu/Continue/History/Top Runs/new run; abandon save aktif dengan temporary build |
| Full `BattleSnapshotTests` | **135/135 PASS**, 0 gagal, 0 skip, 80.130 detik | Seluruh regresi Play Mode, termasuk fitur save/recovery, battle, run, history, dan UI yang ada |

`TC_E2E_001` memulai New Expedition dari tombol Main Menu, memilih tombol node yang tersambung sampai boss, melakukan Continue sesudah dua node, lalu memverifikasi RunId/depth/jalur tetap sama. Sesudah Clear, tes memeriksa Summary terbuka, Echo tersimpan, satu record History, checkpoint dihapus, Top Runs memuat RunId, tombol Continue nonaktif, dan run berikutnya memakai RunId baru tanpa modifier lama. `TC_E2E_002` memeriksa konfirmasi New Expedition saat checkpoint masih ada dan memastikan run baru tidak mewarisi temporary modifier atau jalur lama.

**Batas bukti:** kedua tes menggunakan `EndBattle(true)` untuk menuntaskan encounter dalam tes integrasi. Itu memverifikasi pengkabelan menu, map, commit, save, summary dan arsip, bukan kemampuan menyelesaikan combat dengan aksi pemain normal. Tes-tes combat terpisah tetap tercakup pada suite penuh. Tiga Standard Run manual berturut-turut masih diperlukan oleh gerbang produk.

Artefak: `focused-e2e-results.xml`, `full-playmode-results.xml`, `source-hashes.csv`.

## Build Windows

**PASS untuk pembuatan artefak.** Unity `6000.6.0f1` membuat Windows x86_64 Development Build di `Builds/Verification-2026-09-26/SYNGRAVA.exe`. `BuildScript` menyertakan scene `MainMenu` dan `Battle`; log memperlihatkan kedua scene dibuka saat build. Unity melaporkan ukuran keseluruhan **332.420.599 byte**, durasi **736,97 detik**, **0 error**, dan **514 warning**. Folder verifikasi terpisah dari `Builds/Windows` yang sudah ada. Log lengkap: `Builds/Verification-2026-09-26/unity-build.log`.

Artefak lengkap berisi 357 file (337.880.626 byte), termasuk `SYNGRAVA_Data` dan `UnityPlayer.dll`; jangan memindahkan `.exe` sendirian. SHA-256 `SYNGRAVA.exe`: `718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00`. Angka ukuran BuildPipeline dan total folder berbeda karena yang kedua juga menghitung log smoke/build serta seluruh file keluaran di folder verifikasi.

Warning bukan kegagalan kompilasi: log menunjukkan empat lokasi `CS0618` pada `MainMenuUI.cs` dan `BattleManager.cs`, masing-masing tercetak ulang pada beberapa fase kompilasi, karena `FindFirstObjectByType` telah deprecated. Sebagian besar warning lain muncul saat kompilasi varian shader/compute `Sentis` dari paket AI; contohnya `ConvGeneric` dan `Hidden/Sentis/Pad`. Build tetap memerlukan inspeksi interaktif sebelum dinyatakan siap rilis.

**Startup smoke terbatas:** executable diluncurkan tersembunyi dengan `-batchmode` dan log terpisah; setelah 55 detik proses masih hidup dan log telah melewati inisialisasi engine, Direct3D 12 pada RTX 4050, dan reload assembly tanpa baris exception/error. Proses uji lalu dihentikan. Log tidak memuat penanda bahwa scene Main Menu selesai tampil, dan mode tersembunyi tidak memverifikasi render/input. Bukti: `Builds/Verification-2026-09-26/player-startup-smoke-60s.log`. Startup visual serta interaksi tetap **NOT RUN**.

## Gerbang yang masih terbuka

Jalankan build secara interaktif pada Windows dan isi `Docs/StandardRun_ReleaseGate_Checklist.md`. Uji tiga Standard Run lengkap secara berurutan tanpa debug/skip combat, lalu cek Continue/Recovery, Clear, Defeat, dan Abandon pada profil uji. Jangan menandai gerbang Infinite Casual selesai hanya karena XML hijau atau executable berhasil dibuat.
