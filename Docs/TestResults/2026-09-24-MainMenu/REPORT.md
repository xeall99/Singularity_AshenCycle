# Main Menu SYNGRAVA — 24 September 2026

## Hasil

Main Menu kini memakai komposisi dua area pada panel 1450×820: identitas SYNGRAVA, subjudul, dan simbol mata pada kiri; tindakan utama dan navigasi lain pada kanan. **Continue Journey** dan **New Expedition** mendapat ukuran paling besar. **Echo Archive**, **Run History**, **Settings**, dan **Quit Game** tetap terlihat sebagai empat pilihan pendukung. Seluruh handler tombol, pengecekan checkpoint, konfirmasi New Run, pembelian Echo Prism, Run History, dan Settings dipertahankan.

Panel dibuat saat runtime oleh `MainMenuUI`, memakai backdrop, sprite `CurrentMarker` bermotif mata, dan ornamen tepi yang sudah tersedia pada `StandardRunMapVisualAssets`. Crop mata dari atlas yang tampak berkotak pada render pertama diganti dengan sprite transparan resmi. Tidak ada aset baru, scene/prefab edit, package, atau Inspector assignment.

## Pemeriksaan visual dan alur

- Render Unity final pada **1920×1080** dan **1280×1024** ditinjau. Judul, enam tombol, ringkasan save, pemisah, emblem, dan footer berada dalam panel tanpa teks terpotong. Bukti: `main-menu-1920x1080.png` dan `main-menu-1280x1024.png`.
- Tanpa checkpoint, Continue tampil tidak aktif dengan status **NO SAVED EXPEDITION FOUND**. Dengan checkpoint aktif, Continue aktif dan menampilkan ringkasan stage/resource; memilih New Expedition membuka konfirmasi yang dapat dibatalkan tanpa membuang save.
- Tes menu memeriksa keenam tombol, batas label, input state, dan alur konfirmasi. Suite Play Mode terisolasi final: **124/124 PASS**, **0 failed**, **0 skipped**, durasi 66,4069183 detik (`final-results.xml`). Hash dua file source/test workspace sama dengan salinan Unity yang lulus (`source-hashes.csv`).
- Run awal sebelum perbaikan fixture: **122/123**, karena tes baru salah mengira setup battle belum menulis checkpoint. Fixture dibuat menghapus checkpoint secara eksplisit untuk skenario tanpa save. Tes terarah setelah koreksi: **1/1 PASS**; run final seluruh suite: **124/124 PASS**. Aturan produksi checkpoint tidak diubah untuk mengatasi kegagalan fixture.

## Desain dan batas klaim

Rencana sebelum implementasi tersimpan di `Docs/MainMenu_DesignPlan.md`. File Figma SYNGRAVA berisi map, Run Summary, dan Echo Archive, tetapi belum ada frame Main Menu. Pembacaan detail lanjutan diblokir oleh batas panggilan MCP paket Figma Starter; pencarian desain SYNGRAVA yang tersedia di Canva tidak menghasilkan desain. Karena itu visual final divalidasi dari render Unity, dan tidak diklaim telah disetujui di Figma/Canva.

Belum ada Windows standalone build atau sesi manual dengan mouse/gamepad di mesin pengguna. Layout diuji pada dua resolusi desktop di atas; aspek rasio ekstrem belum diuji. Tidak ada commit atau push. Perubahan lain yang sudah ada pada workspace dipertahankan.
