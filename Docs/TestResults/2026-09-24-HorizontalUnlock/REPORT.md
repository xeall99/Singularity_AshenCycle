# Run Summary polish dan Horizontal Unlock pertama — 24 September 2026

## Hasil

Milestone ini selesai pada kode Unity dan lulus suite Play Mode terisolasi **118/118** (0 gagal, 0 skipped; 65,9695361 detik). Sembilan source/test file inti pada workspace memiliki SHA-256 yang sama dengan salinan proyek yang menghasilkan hasil tes; lihat `source-hashes.csv`. Tidak ada perubahan scene, prefab, database, atau Inspector assignment yang diperlukan.

## Koreksi visual Run Summary

- Judul `FINAL RESOURCES`, `COMBAT RECORD`, dan `BUILD ARCHIVE` dipindahkan masuk ke batas panel masing-masing. Baris Combat terakhir dan rincian Echo diberi ruang agar tidak bertabrakan dengan tepi.
- Sepuluh penanda node diberi bidang gelap dan ikon putih kontras di atas rail yang lebih tenang. Ikon tetap mengikuti node yang benar-benar ditempuh.
- Teks rute berulang dan `RUN SEED` di footer disembunyikan dari pemain. Data masih tersimpan pada snapshot untuk kebutuhan teknis dan kompatibilitas tes; rail visual tetap menjadi riwayat rute yang terlihat.
- Tombol `BEGIN NEW JOURNEY` dipusatkan di footer. Tampilan aktual diperiksa pada 1920×1080 dan 1280×1024; empat PNG di folder ini berasal dari render Unity, bukan mockup.
- [Figma Run Summary](https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=9-14) diperbarui sesuai perubahan hierarki dan tata letak tersebut.

## Horizontal Unlock: Echo Prism

- `EchoFragmentWallet` sekarang memakai wallet versi 2: ledger klaim run dan ledger pembelian permanen. Saldo diverifikasi sebagai jumlah klaim dikurangi biaya pembelian. Wallet versi 1 tetap dapat dibaca dan dimigrasikan saat penulisan berikutnya. Pembelian yang sama kedua kali tidak mengurangi saldo lagi; ID tidak dikenal, ledger rusak, atau saldo tidak cukup ditolak.
- `HorizontalUnlockCatalog` mendaftarkan **Echo Prism** seharga **120 Echo Fragments**. Angka ini adalah baseline balance awal, bukan keputusan akhir playtest ekonomi.
- `ECHO ARCHIVE` pada Main Menu menampilkan saldo, deskripsi, biaya, status kepemilikan, dan tombol pembelian. Pembelian dinonaktifkan ketika checkpoint Active Run masih dapat dilanjutkan agar pool pilihan di tengah run tidak berubah.
- Pembelian hanya menambahkan Echo Prism ke pool kartu Relic Shrine untuk run berikutnya. Ia tidak memberi Base Stats atau item otomatis. Echo Prism adalah Relic unik yang memberi **+4 damage pada Basic Attack pertama setiap battle**. Efek reset di awal battle dan tidak menumpuk dengan dirinya sendiri.
- Main Menu sekarang membangun UI ketika scene menu dimuat setelah game berjalan, bukan hanya pada launch awal. EventSystem menu memakai `InputSystemUIInputModule` sesuai Player Settings; modul input lama dinonaktifkan bila sudah ada. Ini memperbaiki exception input yang ditemukan oleh tes pertama.
- [Figma Echo Archive](https://www.figma.com/design/cFuAmGd1jgxacnD53crytC?node-id=15-14) dibuat sebagai rancangan struktur panel. Panggilan preview Figma berikutnya terkena batas akun; visual final diperiksa dari PNG render Unity dalam folder ini.

## Verifikasi

1. Run awal salinan bersih: **117/118**. Tes menu menunjukkan bootstrap tidak terjadi saat scene menu dimuat belakangan. Perbaikan bootstrap menemukan masalah kedua: modul input lama memicu exception pada proyek Input System.
2. Tes terarah menu setelah perbaikan: **1/1 PASS**, termasuk render Echo Archive 1920×1080 dan 1280×1024. Kedua gambar diperiksa secara visual; label, tombol, dan kartu berada dalam batasnya.
3. Run final seluruh `BattleSnapshotTests`: **118/118 PASS**, **0 gagal**, **0 skipped**. Tes baru mencakup persistensi dan idempotensi pembelian, ketidakcukupan saldo, pool kartu sebelum/sesudah unlock, +4 damage hanya pada serangan pertama, modul input menu, containment teks, dan render dua resolusi.
4. `git diff --check` tidak menemukan whitespace error. Tidak ada commit atau push; perubahan pengguna yang sudah ada tetap dipertahankan.

File bukti: `final-results.xml`, `source-hashes.csv`, `run-summary-1920x1080.png`, `run-summary-1280x1024.png`, `echo-archive-1920x1080.png`, dan `echo-archive-1280x1024.png`.

## Batas dan langkah terkecil berikutnya

Tes otomatis dan render tidak menggantikan playtest manual di Unity Editor maupun Windows standalone build. Pembacaan wallet v1 belum memiliki tes migrasi khusus walaupun jalurnya ada dan telah ditinjau pada kode. Biaya 120 dan bonus +4 perlu evaluasi balance setelah beberapa run nyata. Milestone berikutnya yang aman adalah **Run History lokal**: simpan snapshot hasil run yang sudah final secara idempotent, tampilkan daftar ringkas Clear/Defeat beserta tanggal, seed, rute, dan build, lalu uji kompatibilitas serta kegagalan tulis. Skor dan leaderboard sebaiknya menunggu kontrak ScoreCalculator yang jelas.
