# Draft Katalog 15 Skill Pemain — SYNGRAVA

**Status:** draf historis 15 skill; spesifikasi runtime yang berlaku adalah roster 30 skill pada [`Skill_Collection_Loadout_DesignPlan.md`](Skill_Collection_Loadout_DesignPlan.md). Jangan gunakan efek lama di dokumen ini sebagai perilaku game saat ini.  
**Versi:** 0.3 — 26 Oktober 2026

## Arah desain

Skill adalah pilihan resmi yang ditulis dan diseimbangkan oleh game. Pemain mengumpulkannya lalu memilih skill melalui loadout; pemain tidak membuat nama, damage, biaya Mana, atau efek sendiri. Setiap skill punya efek aktif, passive yang hanya bekerja selama skill itu dipasang, dan fungsi yang memberi alasan berbeda untuk dipilih.

Inspirasi diambil dari pola permainan tingkat tinggi seperti boon yang membentuk kombinasi, serangan balasan, status bertahap, dan sinergi build. Nama, angka, teks, dan susunan efek di bawah dibuat untuk SYNGRAVA; ini bukan salinan skill atau tooltip game lain.

## Aturan angka dan pemicu

- Damage yang dicantumkan adalah damage dasar sebelum critical, bonus Orbit, modifier run, Anomaly, Relic, dan pengali status. Nilai ini memakai skala prototype sekarang: Basic Attack 10 damage, Shadow Strike 20 Power, Mana maksimum 10, dan Mana awal 5.
- Semua skill sukses mengisi satu slot Orbit dan mengakhiri turn pemain seperti aksi Skill saat ini. Biaya Mana yang tidak terpenuhi berarti aksi gagal dan tidak memberi slot Orbit.
- Passive aktif selama skill ada di loadout, dan setiap passive di bawah hanya dapat terpanggil sekali per battle. Efek healing tidak melewati HP maksimum; pemulihan Mana tidak melewati batas Mana.
- Jika skill multi-hit, angka adalah total satu cast dan hanya memakai satu hasil critical. Implementasi perlu mempertahankan urutan yang jelas terhadap Burn, Curse, dan efek Orbit.
- Biaya dan damage adalah titik awal playtest, bukan angka balance final. Semua 15 skill masih memerlukan uji kombinasi, terutama saat dipasangkan dengan Event Horizon, critical, Relic, dan modifier run.

## Roster

### 1. `SKL_SHADOW_STRIKE` — finisher pembuka

- **Aktif — 2 Mana:** Serang satu musuh untuk **20 damage**.
- **Passive — Bidikan dari Analisis:** Sekali per battle, jika target sudah diungkap oleh Analyze, Shadow Strike mendapat **+4 damage**.
- **Kegunaan:** Skill serang dasar yang tetap relevan; Analyze memberi alasan taktis untuk memilih waktu pemakaian.
- **Inspirasi pola:** bonus bersyarat dari boon Hades, diubah menjadi payoff atas informasi Analyze.

### 2. `SKL_ASHWAKE_CANTICLE` — Burn dan sustain

- **Aktif — 1 Mana:** Beri **6 damage** dan terapkan 1 stack Burn selama 2 turn musuh.
- **Passive — Bara Pertama:** Sekali per battle, tick pertama Burn yang dipasang oleh skill ini memulihkan **1 Mana**.
- **Kegunaan:** Tekanan murah untuk battle panjang; Burn memberi damage setelah aksi selesai dan passive mengembalikan sebagian resource.
- **Inspirasi pola:** damage bertahap dan kombinasi boon Hades.

### 3. `SKL_GRAVEMUTE_SEAL` — kendali serangan musuh

- **Aktif — 2 Mana:** Beri **4 damage** dan terapkan Weakness selama 2 turn musuh.
- **Passive — Gema Peredam:** Sekali per battle, serangan pertama musuh saat terkena Weakness menerima pengurangan **3 damage** tambahan.
- **Kegunaan:** Mengurangi tekanan serangan sambil tetap memberi sedikit damage; paling berguna sebelum serangan musuh yang kuat.
- **Inspirasi pola:** debuff yang membentuk rencana giliran seperti efek kartu di Slay the Spire.

### 4. `SKL_VESPER_STITCH` — pemulihan dan cleanse

- **Aktif — 3 Mana:** Pulihkan **8 HP** dan hapus satu Weakness atau Curse dari pemain. Tidak memberi damage.
- **Passive — Jahitan Darurat:** Sekali per battle, saat HP pemain 40% atau kurang, biaya aktif Vesper Stitch berkurang **1 Mana**.
- **Kegunaan:** Memberi pilihan untuk bertahan di run panjang; manfaat utamanya adalah HP dan pembersihan status, bukan serangan.
- **Inspirasi pola:** pilihan pemulihan dan pengelolaan resource dalam build Slay the Spire.

### 5. `SKL_PALE_LEDGER` — Curse dan kombo Orbit

- **Aktif — 2 Mana:** Beri **6 damage** dan terapkan Curse selama 2 turn musuh.
- **Passive — Hutang Orbit:** Sekali per battle, Event Horizon yang mengenai musuh yang masih terkena Curse memulihkan **1 Mana** setelah damage selesai.
- **Kegunaan:** Menyiapkan target untuk skill berikutnya dan memberi payoff pada urutan A-A-S.
- **Inspirasi pola:** setup status dan payoff kombinasi boon Hades.

### 6. `SKL_BLACKGLASS_PARRY` — parry dan counter

- **Aktif — 3 Mana:** Beri **5 damage** dan siapkan parry sampai aksi musuh berikutnya. Serangan langsung pertama dikurangi 50%; setengah damage yang dicegah dipantulkan ke musuh.
- **Passive — Retak Balik:** Sekali per battle, counter dari Reversal Orbit juga menerapkan Weakness selama 1 turn musuh.
- **Kegunaan:** Membaca intent lalu menukar satu aksi menyerang dengan perlindungan dan peluang balasan.
- **Inspirasi pola:** deflect dan counterattack Sekiro, dipetakan ke aksi Guard dan Reversal Orbit.

### 7. `SKL_HOLLOW_CONDUIT` — konversi Mana

- **Aktif — 2 Mana:** Pulihkan **4 Mana**. Tidak memberi damage; aksi tetap memakai turn dan slot Orbit, sehingga menghasilkan hingga 2 Mana bersih sebelum batas maksimum.
- **Passive — Arus Sisa:** Unstable Pulse pertama saat Hollow Conduit terpasang memulihkan **1 Mana tambahan**.
- **Kegunaan:** Menyiapkan resource untuk skill mahal. Pemain perlu memilih timing karena skill ini mengorbankan satu aksi.
- **Inspirasi pola:** pengelolaan resource dan kombinasi boon Hades.

### 8. `SKL_CINDER_REFRAIN` — serangan bertubi pada target terbakar

- **Aktif — 2 Mana:** Dua tebasan, masing-masing **6 damage**; total dasar **12 damage**.
- **Passive — Bara Bersahut:** Sekali per battle, jika target sudah terkena Burn sebelum skill dipakai, tebasan kedua mendapat **+4 damage**.
- **Kegunaan:** Serangan langsung untuk build Burn. Menunggu Burn aktif menghasilkan cast yang lebih kuat.
- **Inspirasi pola:** serangan beruntun dan upgrade bersyarat pada senjata Hades.

### 9. `SKL_RIFT_SIPHON` — lifesteal dan penyelesaian

- **Aktif — 4 Mana:** Beri **14 damage**; jika target terkena Curse saat cast dimulai, pulihkan **4 HP**.
- **Passive — Rezeki dari Retakan:** Sekali per battle, jika Rift Siphon mengalahkan musuh, pulihkan **4 HP** tambahan.
- **Kegunaan:** Serangan mahal yang memberi pemulihan jika pemain menyiapkan Curse atau berhasil menghabisi target.
- **Inspirasi pola:** efek sustain yang dipadukan dengan boon/build Hades.

### 10. `SKL_BLACKSTAR_BRAND` — tandai lalu serang

- **Aktif — 2 Mana:** Beri **7 damage** dan tandai target selama 2 turn musuh. Basic Attack pemain berikutnya terhadap target itu mendapat **+8 damage** dan menghabiskan tanda.
- **Passive — Jejak yang Tertinggal:** Sekali per battle, jika target bertanda bertahan hidup setelah Basic Attack berikutnya, target terkena Weakness selama 1 turn musuh.
- **Kegunaan:** Mengatur urutan Skill lalu Attack; tanda memberi bonus yang jelas dan mendorong pemain memanfaatkan Orbit.
- **Inspirasi pola:** setup status lalu payoff serangan dalam build Slay the Spire.

### 11. `SKL_VOW_OF_ASH` — tukar HP untuk damage

- **Aktif — 1 Mana:** Beri **15 damage**, lalu pemain kehilangan **3 HP**. Skill tidak dapat digunakan jika HP pemain 3 atau kurang.
- **Passive — Harga yang Dibayar:** Sekali per battle, jika Vow of Ash mengalahkan musuh, pulihkan **3 HP**.
- **Kegunaan:** Burst murah dengan risiko nyata; paling efektif saat pemain yakin damage-nya akan menutup battle.
- **Inspirasi pola:** pilihan high-risk/high-reward pada build roguelike aksi seperti Hades.

### 12. `SKL_GALLOWS_BLOOM` — execute

- **Aktif — 4 Mana:** Beri **11 damage**; jika HP target 30% atau kurang, beri **11 damage tambahan**.
- **Passive — Panen Terakhir:** Sekali per battle, jika Gallows Bloom mengalahkan target, pulihkan **1 Mana**.
- **Kegunaan:** Menahan skill ini sampai target melemah, lalu mengubahnya menjadi alat penutup battle.
- **Inspirasi pola:** bonus damage bersyarat pada boon Hades, diubah menjadi payoff terhadap target sekarat.

### 13. `SKL_MIRROR_WARDEN` — pertahanan berlapis

- **Aktif — 2 Mana:** Beri **8 damage** dan siapkan pelindung yang mencegah hingga **5 damage** dari serangan langsung berikutnya.
- **Passive — Pantulan Terkendali:** Sekali per battle, jika pelindung mencegah damage, Basic Attack berikutnya mendapat **+4 damage**.
- **Kegunaan:** Lebih ringan daripada parry; cocok untuk pemain yang ingin tetap menyerang sambil menyerap sebagian pukulan.
- **Inspirasi pola:** pertahanan yang membuka jendela balasan seperti pola deflect Sekiro.

### 14. `SKL_ECHO_SCRIPT` — urutan aksi dan gema damage

- **Aktif — 4 Mana:** Beri **9 damage** ditambah setengah damage dari Basic Attack terakhir yang berhasil, dibulatkan ke bawah dan dibatasi bonus maksimum **5 damage**.
- **Passive — Tulisan Sesudah Guard:** Sekali per battle, jika aksi pemain sebelumnya adalah Guard, biaya Echo Script berkurang **1 Mana**.
- **Kegunaan:** Menghargai pemain yang mengganti ritme antara Guard, Attack, dan Skill; nilainya bergantung pada aksi sebelumnya.
- **Inspirasi pola:** sinergi urutan dan efek antar-pilihan di Slay the Spire.

### 15. `SKL_ABYSSAL_VERDICT` — pecahkan tumpukan Burn

- **Aktif — 5 Mana:** Beri **12 damage**, lalu konsumsi semua stack Burn pada target untuk memberi **4 damage per stack**. Dengan 3 stack, total damage dasar cast menjadi **24**.
- **Passive — Abu yang Menyebar:** Sekali per battle, jika target selamat dari Abyssal Verdict, target terkena Weakness selama 1 turn musuh.
- **Kegunaan:** Finisher mahal untuk build Burn. Pemain memilih antara membiarkan Burn terus tick atau memecahkannya sekarang untuk burst.
- **Inspirasi pola:** menyiapkan status dan membayar tumpukannya sebagai burst, mengikuti pola sinergi boon Hades.

## Koleksi, loadout, dan reward wave

1. Katalog berisi skill resmi yang didefinisikan developer. Koleksi pemain hanya mencatat skill yang telah dimiliki; ia tidak menyimpan angka buatan pemain.
2. Setiap checkpoint 10 wave di Infinite, Legend, atau mode khusus yang mendukungnya menampilkan 3 pilihan dari katalog yang memenuhi syarat.
3. Pilihan menambah skill ke build run saat itu dan membuka skill tersebut di koleksi untuk loadout run berikutnya.
4. Loadout sebelum run memilih skill yang akan dibawa. Jumlah slot tetap untuk MVP, tetapi angka pastinya belum ditetapkan.
5. Aturan untuk pilihan yang sudah dimiliki dan ukuran minimum katalog agar tiga pilihan tetap valid perlu ditentukan sebelum implementasi.

## Main Menu dan Custom Skill

Custom Skill CRUD tidak menjadi fitur yang dapat dibuka pemain dari Main Menu. Perubahan yang diminta menghapus tombol dan inisialisasi panel tersebut dari alur Main Menu; repository SQLite serta pengujian internal CRUD dapat tetap ada sebagai bukti UKK. Record bebas yang dibuat lewat demo tidak masuk ke combat, koleksi pemain, loadout, atau reward wave. Semua pilihan pemain harus merujuk pada katalog resmi yang dibatasi.

## Catatan implementasi sebelum skill masuk ke game

- Prototype saat ini hanya memuat Shadow Strike 20 Power / 2 Mana; inventory, loadout, reward wave, dan passive belum ada.
- Burn, Weakness, Curse, Orbit, Analyze, serta damage dasar sudah menjadi bagian sistem saat ini. Mark, pelindung flat, reflect, healing dari skill, self-cost HP, konsumsi Burn, dan pengaturan multi-hit memerlukan combat hook baru.
- Skill harus mengalir melalui satu jalur validasi Mana, damage, critical, status, Orbit, statistik run, dan save. Jangan biarkan UI reward atau database Content Manager mengubah damage aktif di luar katalog.
- Sebelum coding, tetapkan slot loadout, aturan duplicate offers, stacking / reapply status, prioritas pasif terhadap Orbit, dan perlakuan boss terhadap efek kontrol.

## Rujukan inspirasi mekanik

- [Hades — The Blood Price Update](https://www.supergiantgames.com/blog/hades-the-blood-price-update-patch-notes/) menunjukkan ragam boon dan upgrade yang memberi bonus bersyarat, serangan beruntun, serta counterattack.
- [Sekiro — Game Mechanics](https://support.activision.com/tw/zh/sekiro/articles/sekiro-shadows-die-twice-game-mechanics) menjelaskan hubungan menyerang/deflect dengan posture dan counterattack.
- [Slay the Spire di Steam](https://store.steampowered.com/app/646570/_/_/) menjadi rujukan tingkat struktur untuk membentuk build dari kombinasi pilihan dan relic.
