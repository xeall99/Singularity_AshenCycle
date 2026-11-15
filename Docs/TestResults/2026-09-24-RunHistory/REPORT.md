# Run History lokal — 24 September 2026

## Hasil

Run History lokal sudah terpasang pada Main Menu sebagai **RUN HISTORY / THREAD ARCHIVE**. Arsip menampilkan hasil Standard Run yang selesai (Cleared atau Defeated), terbaru lebih dulu, enam entri per halaman. Detailnya berisi waktu lokal, node yang diselesaikan, seed, durasi, HP, Mana, Embers, Echo Fragments yang didapat, statistik pertempuran, build, dan rute. Arsip hanya untuk dibaca; membuka halaman atau memilih entri tidak mengubah run maupun resource.

Hasil akhir salinan proyek Unity terisolasi: **122/122 Play Mode PASS**, **0 failed**, **0 skipped**, durasi 68,3423874 detik. Sembilan file source/test yang diverifikasi memiliki SHA-256 identik antara workspace dan proyek yang diuji (`source-hashes.csv`). Tidak ada perubahan scene, prefab, package, atau Inspector assignment.

## Kontrak penyimpanan

- `RunHistoryStore` menyimpan arsip di `Application.persistentDataPath/Saves/syngrava-run-history-v1.json`. Format v1 membungkus payload JSON dengan SHA-256. Penulisan menggunakan file sementara dan backup; pembacaan mencoba backup ketika primary rusak. Data rusak ditolak, dan UI memberi status arsip tidak tersedia alih-alih mengubah save lain.
- Satu `RunId` hanya menghasilkan satu rekaman. Run yang masih aktif ditolak. Hanya snapshot terminal setelah klaim Echo berhasil yang dapat direkam. Pencatatan sejarah terjadi sebelum checkpoint Active Run dihapus; bila penulisan gagal, layar hasil run memberi pesan dan tombol retry. Penyimpanan Echo, checkpoint Active Run, dan History tetap memiliki tanggung jawab terpisah.
- Rekaman dibatasi 10.000 entri untuk mencegah arsip tumbuh tanpa batas. Archive hanya berisi ringkasan hasil dan rute, bukan checkpoint yang dapat dipakai untuk melanjutkan run.

## UI dan pemeriksaan visual

Panel memakai bidang gelap, garis violet, teks putih, dan aksen merah untuk Defeat sesuai visual SYNGRAVA saat ini. Setelah render pertama, panel daftar/detail diturunkan agar subjudul tidak menyentuh tepi; enam baris disusun ulang. Rute panjang ditata dalam dua baris yang seimbang agar separator tidak tertinggal sendirian saat layar lebih sempit. Tombol halaman yang tidak aktif dibuat lebih redup. Render Unity terakhir pada **1920×1080** dan **1280×1024** diperiksa secara visual; semua label dan panel berada di dalam batasnya. Bukti: `run-history-1920x1080.png` dan `run-history-1280x1024.png`.

## Pengujian dan koreksi

1. Run awal setelah implementasi inti: **122/122 PASS** (`initial-results.xml`).
2. Setelah tes render ditambahkan, run visual: **121/122** (`visual-review-results.xml`). Satu tes combat lama mengasumsikan Shadow Strike selalu menghasilkan damage noncritical; critical hit acak yang sah membuat asersi gagal. Tes diperbaiki agar menerima hasil sesuai perhitungan critical yang benar.
3. Setelah pemolesan rute, run berikutnya: **121/122** (`route-polish-results.xml`). Tes lama Curse membuat asumsi noncritical yang sama untuk Basic Attack. Aturan combat tidak diubah; asersi tes dikoreksi memakai jumlah critical hit yang tercatat.
4. Run final pada source dan test yang sama dengan workspace: **122/122 PASS** (`final-results.xml`). Tes baru mencakup idempotensi `RunId`, urutan terbaru, read-only copy, pemulihan dari backup, kegagalan penulisan, commit pada akhir battle sebelum checkpoint dihapus, pagination Main Menu, dan containment/render dua resolusi.
5. `git diff --check` untuk file terlacak yang disentuh tidak menunjukkan whitespace error. Tidak ada commit atau push. Perubahan pengguna lain pada workspace dipertahankan.

## Batas klaim dan langkah berikutnya

Belum ada Windows standalone build atau sesi main manual dari awal sampai akhir. Backup bisa berada satu penulisan di belakang primary bila primary rusak tepat setelah commit terbaru; UI menampilkan data valid terakhir yang tersedia. Arsip belum memiliki score, filter, penghapusan entri, cloud sync, atau leaderboard. Langkah terkecil berikutnya ialah mendefinisikan **ScoreCalculator** dari data hasil run yang sudah disimpan, lalu menambah skor ke riwayat setelah aturan skornya disetujui.
