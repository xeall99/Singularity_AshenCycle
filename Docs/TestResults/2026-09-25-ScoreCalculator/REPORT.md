# Standard Run ScoreCalculator — 25 September 2026

## Hasil

Skor Standard Run sekarang berasal dari node yang benar-benar diselesaikan dan Convergence yang memenuhi batas per battle. Run Summary menampilkan total; Run History menampilkan total dan rincian per sumber. Riwayat yang tersimpan sebelum model skor ini tetap terbaca, tetapi ditandai **UNSCORED** karena angka lamanya tidak dapat direkonstruksi secara akurat.

Rumus model 1 mengikuti nilai dasar GDD dengan pengali kesulitan 1 untuk Standard Run: Normal Battle 100, Random Event 75, Elite 500, Boss 1.500, dan Named Convergence 50 untuk paling banyak tiga pemicu pada **setiap battle yang diselesaikan**. Rest, Merchant, Relic, damage, healing, waktu, Embers, Echo, dan panjang turn tidak menambah skor. Konvergensi dalam battle yang berakhir Defeat tidak menghasilkan bonus. Pilihan nilai dan pengecualian dicatat sebelum implementasi di `Docs/Score_Model.md`.

## Perubahan dan kompatibilitas

- `ScoreCalculator` murni menghasilkan `ScoreResult` beserta Battle, Event, Elite, Boss, dan Convergence Points. Penghitungan tidak memakai UI, disk, waktu, atau randomizer.
- `RunManager` menyimpan total Convergence untuk ringkasan lama, jumlah yang layak skor dari battle selesai, serta penghitung battle berjalan yang dibatasi tiga. Penghitung battle berjalan direset ketika node terselesaikan. `SeededRunState` menyimpan kedua penghitung baru dan memvalidasi batasnya. Field tambahan ini tetap memakai schema checkpoint 3; checkpoint lama dibaca dengan nilai nol untuk field baru, tanpa menebak histori battle masa lalu.
- `RunHistoryRecord` menyimpan skor, versi model, dan rincian. Format arsip tetap v1 karena field tambahan dibaca secara opsional. Rekaman lama dengan versi skor nol tetap valid dan tidak diberi skor palsu. `RunHistoryStore` menolak rincian poin yang negatif atau total yang tidak sama dengan penjumlahan rinciannya.
- `RunSummaryUI` menampilkan total skor di blok status kanan atas. `RunHistoryUI` menampilkan skor ringkas pada daftar dan breakdown pada detail. Tidak ada perubahan scene, prefab, paket, database SQLite, atau assignment Inspector.

## Verifikasi

- Kompilasi produksi lokal: **0 error**, empat warning `FindFirstObjectByType` yang sudah ada sebelum perubahan skor. Kompilasi assembly tes: **0 error, 0 warning**.
- Play Mode pada salinan proyek terisolasi: run pertama **127/127 PASS**, run final setelah menambah asersi cap untuk battle berikutnya **127/127 PASS**, 0 failed/skipped. Hasil ada di `first-results.xml` dan `final-results.xml`.
- Tes baru menguji determinisme dan rincian poin, pengecualian statistik yang bisa di-farm, cap tiga pada satu battle, cap baru pada battle berikutnya, round-trip checkpoint, penolakan counter rusak, Defeat tanpa poin battle yang belum selesai, serta arsip lama tanpa skor. Tes Run History yang ada juga memeriksa persistensi versi skor dan breakdown UI.
- Delapan source/test file yang dibandingkan memiliki SHA-256 identik antara workspace dan proyek Unity terisolasi (`source-hashes.csv`). Render final pada 1920×1080 dan 1280×1024 untuk Run Summary dan Run History diperiksa secara visual; skor, breakdown, tombol, dan panel tetap terbaca tanpa teks keluar batas. PNG terlampir di folder laporan.
- Percobaan Play Mode pertama dalam sandbox tertahan oleh Unity Licensing Client sebelum menghasilkan XML. Peluncuran kedua dengan akses lokal menyelesaikan pengujian; error lisensi awal tidak dihitung sebagai tes gagal.

## Batas dan langkah selanjutnya

Belum ada Windows standalone build, playtest manual penuh, atau penetapan akhir balance skor oleh pemilik. Model ini berlaku untuk Standard Run; Infinite Casual/Ranked dan multiplier kesulitan belum diimplementasikan. Arsip lama sengaja tidak diberi skor retroaktif. Local Leaderboard belum dibuat. Langkah kecil berikutnya adalah membaca skor dari Run History untuk menyusun peringkat lokal top 10 dengan tie-break depth, boss count, lalu durasi, tanpa membuat save hasil baru yang bisa menduplikasi `RunId`.
