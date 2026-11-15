# Main Menu SYNGRAVA — rencana tata letak

Tanggal: 24 September 2026. Rencana ini mendahului perubahan UI. Figma SYNGRAVA sudah memiliki frame map, Run Summary, dan Echo Archive, tetapi belum memiliki frame Main Menu. Akses detail Figma saat ini tertahan batas panggilan MCP paket Starter. Pencarian desain SYNGRAVA milik pengguna di Canva tidak menemukan hasil. Karena itu implementasi akan memakai sprite, palet, dan komponen yang sudah ada di proyek, lalu diverifikasi melalui render Unity.

## Tujuan

- Nama game menjadi fokus pertama, dengan motif mata/kristal SYNGRAVA dari atlas ornament yang sudah tersedia.
- Pemain langsung melihat dua tindakan utama: **Continue Journey** dan **New Expedition**. Continue menampilkan kondisi save yang benar dan jelas saat tidak tersedia.
- Echo Archive, Run History, Settings, dan Quit tetap mudah ditemukan tetapi tidak bersaing ukuran dengan tindakan utama.
- Konfirmasi sebelum mengganti Active Run, pembelian Echo Prism, pembacaan Run History, serta navigasi ke Battle tetap menggunakan alur yang sudah ada.

## Komposisi pada referensi 1920×1080

- Satu bidang utama lebar sekitar 1450×800, berpusat, dengan latar hitam-violet dan garis tipis. Tekstur retak/torn-paper tetap berfungsi sebagai aksen, bukan lapisan di atas teks.
- Sisi kiri (sekitar 40%): judul **SYNGRAVA**, subjudul **ASHEN CYCLE**, motif mata dan pecahan kristal, satu kalimat atmosfer pendek. Sisakan ruang kosong di sekeliling judul agar terbaca.
- Sisi kanan (sekitar 60%): dua tombol utama tersusun vertikal, lalu dua baris tombol sekunder berpasangan. Teks tombol utama rata kiri dengan ringkasan save di bawahnya; tombol sekunder cukup satu baris.
- Footer kecil berisi versi/proyek. Garis pemisah tipis membedakan area identitas dari area navigasi.
- Pada 1280×1024, seluruh panel, teks, tombol, dan dialog tetap berada dalam layar. Tidak boleh ada teks terpotong atau bertumpuk.

## Warna dan interaksi

- Dasar hitam-ungu, permukaan panel ungu sangat gelap, putih untuk teks utama, violet untuk pilihan utama/fokus, merah hanya untuk peringatan atau keputusan destruktif.
- State hover/focus/disabled harus berbeda. Continue tanpa checkpoint tetap terlihat sebagai pilihan yang tidak tersedia, bukan sebagai tombol rusak.
- Modal konfirmasi, Echo Archive, Run History, dan Settings berada di atas menu dan bisa ditutup tanpa mengubah state run.

## Penerimaan dan tes

1. Render menu pada 1920×1080 dan 1280×1024, tinjau hierarki, kontras, jarak, dan batas panel.
2. Periksa tombol dan label dengan/tanpa Active Run checkpoint, serta navigasi Continue/New Run dan pembukaan empat pilihan sekunder.
3. Jalankan suite Play Mode setelah perubahan kode selesai; laporkan jumlah lulus dan batas klaim. Tidak perlu scene, prefab, atau Inspector assignment baru bila layout runtime mencukupi.
