# Laporan Skill Archive dan Audit Passive

**Tanggal:** 15 November 2026  
**Unity:** 6000.6.0f1  
**Status:** tiga regresi Play Mode berturut-turut lulus. Nilai damage dan Mana masih perlu playtest balance pemilik.

## Hasil verifikasi

Suite `BattleSnapshotTests` berjalan tiga kali berurutan di proyek Unity salinan terisolasi. Setiap pass memakai `companyName` berbeda sehingga direktori `Application.persistentDataPath` tidak berbagi save. Workspace Unity utama dan save pemain tidak dipakai oleh runner.

| Pass | Profil terisolasi | Total | Lulus | Gagal | Skip | Durasi suite |
|---:|---|---:|---:|---:|---:|---:|
| 1 | `SYNGRAVA_SkillFinalPass1_20261115` | 168 | 168 | 0 | 0 | 142.2548038 dtk |
| 2 | `SYNGRAVA_SkillFinalPass2_20261115` | 168 | 168 | 0 | 0 | 148.7598559 dtk |
| 3 | `SYNGRAVA_SkillFinalPass3_20261115` | 168 | 168 | 0 | 0 | 135.3762054 dtk |

Unity mengompilasi source tanpa error `CS` pada ketiga log. Log tetap berisi warning lama package/editor dan entitlement lookup 404; semua test tetap selesai dengan `Passed`, tanpa test gagal atau skip.

### Artefak

- `playmode-results-1.xml` dan `unity-playmode-1.log`
- `playmode-results-2.xml` dan `unity-playmode-2.log`
- `playmode-results-3.xml` dan `unity-playmode-3.log`
- `source-hashes.csv` membandingkan SHA-256 delapan source utama di workspace dengan salinan yang diuji. **8/8 cocok**.

## Perubahan roster dan passive

Roster runtime berisi 30 skill kurasi; test memastikan 30 identitas active, 30 identitas passive, serta 30 deskripsi passive berbeda dan tidak kosong. Shadow Strike tetap skill awal. Koleksi, loadout tetap tiga slot, Skill Memory, offer deterministik, claim idempotent, snapshot Run, dan migrasi save sebelumnya tetap berada dalam regresi penuh.

Audit BattleManager memperbaiki mismatch antara deskripsi dengan hasil runtime:

- Cinder Key sekarang mendapat Ember pada konsumsi Burn pertama dan tidak lagi memberi Weakness.
- Abyssal Verdict memberi Curse jika setidaknya dua Burn dikonsumsi dan target masih hidup; tidak lagi memberi Weakness.
- `BattleStatusController.ExtendDuration` menjaga jumlah stack saat memperpanjang Weakness/Curse, dipakai Gravity Thread dan Paper Crown.
- Rift Siphon kini memulihkan HP saat target Cursed mati melalui Burn tick, reflection, atau counter, selain kill langsung.
- Gallows Bloom hanya menerima kill yang langsung dibuat oleh Skill. Basic Attack, Burn tick, parry, dan counter tidak dianggap Skill kill.
- Sinkronisasi legacy resource diperbarui setelah passive Ember Reprise dan heal/resource dari kill.
- Passive tak terpasang tidak memakai jatah one-use. Semua passive satu kali hanya terpakai setelah syarat benar-benar terpenuhi; diskon Echo Script memang berulang jika Guard adalah aksi pemain sebelumnya.

Test skill baru menguji katalog unique, perpanjangan durasi status, Cinder Key, Abyssal Verdict, Guard → Echo Script/Ember Reprise, dan kill Burn terhadap target Cursed untuk Rift Siphon.

### Proses koreksi sebelum rangkaian final

Pass diagnostik awal menemukan setup test yang hanya memasang skill pada array HUD sehingga refresh mengembalikannya ke loadout Run. Test helper diperbaiki agar memasang skill melalui `RunManager`. Pass selanjutnya juga menunjukkan field resource legacy belum disinkronkan setelah Ember Reprise; sinkronisasi ditambahkan. Audit semantik terakhir menemukan Rift Siphon belum merespons kill non-langsung; hook dan satu regression test ditambahkan. Hasil diagnostik tersebut tidak dihitung sebagai acceptance. Tiga XML di tabel adalah satu-satunya rangkaian final dan menguji source yang sama.

## Riset desain

Halaman unit yang diberikan pemilik dicoba secara langsung. `wiki.vanguards.gg` dan `wiki.animeexpeditions.com` merespons HTTP 403; daftar unit Anime Crusaders yang diberikan dapat dibuka tetapi tidak memiliki isi teks yang dapat dianalisis. Karena ability tiap unit tidak berhasil dibaca, tidak ada nama, angka, atau efek spesifik yang diasumsikan berasal dari wiki tersebut.

Riset pengganti memakai [panduan Anime Expeditions berdasarkan rarity](https://allthings.how/anime-expeditions-every-unit-passive-and-active-ability-by-rarity/). Pelajaran yang dipakai adalah memasangkan active yang bisa dipilih dengan passive yang mempunyai trigger/peran terbaca pada unit rarity tinggi; ability, nama, dan nilai SYNGRAVA ditulis sendiri. Balance roster belum dianggap final.

## Dokumen dan batas klaim

- Roster dan aturan Skill Memory diperbarui di `Docs/Skill_Collection_Loadout_DesignPlan.md`.
- `Docs/Infinite_Run_DesignPlan.md` berisi rancangan Infinite Run Casual yang diteliti dan lengkap untuk tahap desain; belum ada implementasi Infinite. Source generator/run-state sekarang hanya mendukung map Standard tunggal.
- `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md` dan kedua salinan GDD v0.5 diperbarui. GDD v0.4 lama dipulihkan dan disimpan sebagai arsip agar tidak hilang.
- Konten dua GDD v0.5 memiliki struktur teks yang sama (642 paragraf dan 75 tabel). Visual render tidak dapat dibuat pada mesin ini: helper resmi `render_docx.py` melaporkan `soffice.exe`/LibreOffice tidak tersedia. Karena itu tata letak akhir GDD belum diperiksa secara visual; tidak ada klaim render-pass.
- Tiga pass menguji regresi otomatis, bukan tiga playthrough pemain. Skill masih butuh penilaian balance/readability oleh pemilik. Infinite Run baru boleh masuk implementasi setelah item tersisa pada `Docs/StandardRun_ReleaseGate_Checklist.md` diterima untuk executable kandidat.
