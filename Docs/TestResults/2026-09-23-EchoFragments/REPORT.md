# Echo Fragments — 23 September 2026

Implementasi: ikon kristal ungu/putih transparan, reward battle sesuai GDD, saldo permanen dengan ledger klaim per run, checkpoint compatible, dan tampilan pada Run Summary. Tidak ada scene atau Inspector assignment baru.

Aturan: Normal 8–12 dari seed dan kode node; Elite 20; Boss 50; Clear menyimpan seluruh temuan plus bonus 20 sementara; Defeat menyimpan 60% temuan dengan pembulatan ke bawah. Hanya node selesai yang dihitung. Ledger melindungi dari klaim ganda setelah Continue/recovery. Kegagalan save dompet mempertahankan checkpoint dan menampilkan retry.

## Verifikasi

Unity 6.6 Play Mode final di salinan proyek terisolasi: **114/114 PASS, 0 failed, 0 skipped**, durasi **64,2978296 detik**. Perintah awal: `Tools/Run-BattleSnapshotTests.ps1`. Setelah satu tes lama gagal secara acak ketika Critical muncul, ekspektasinya diperbaiki agar menggunakan damage Critical aktual; suite lengkap dijalankan ulang di salinan yang sama. Setelah ukuran ikon diperbaiki, suite lengkap dijalankan lagi dan hasil final tetap 114/114.

`TC_ECHO_001` memeriksa reward 8–12, retensi 60%, saldo permanen, klaim berulang, dan run baru dengan seed yang sama. `TC_ECHO_002` memeriksa restore checkpoint schema 3 lama tanpa RunId. `TC_ECHO_003` memeriksa Boss 50, bonus Clear 20, dan jumlah total. Regression Run Summary memeriksa sprite Echo yang dimuat; render 1920×1080 dan 1280×1024 memeriksa overflow/containment teks. Kedua screenshot final diperiksa visual: ikon, saldo, breakdown dan tombol terlihat tanpa teks menumpuk.

Bukti: `final-results.xml`, `echo-summary-1920x1080.png`, `echo-summary-1280x1024.png`, dan `source-hashes.csv`. Hash seluruh 8 source/test/aset yang relevan cocok dengan salinan terisolasi yang menghasilkan hasil final. `git diff --check` tidak menemukan whitespace error pada tracked diff.

## Batas

Bonus clear 20 dan pembulatan ke bawah adalah parameter awal untuk playtest karena GDD belum menetapkan keduanya. Sistem pembelian Horizontal Unlock, histori run permanen, Windows build, dan uji manual panjang belum termasuk milestone ini.
