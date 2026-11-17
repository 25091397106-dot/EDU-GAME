using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [Header("Pengaturan Giliran")]
    public int jumlahPemain;
    public int giliranSekarang = 0;

    [Header("Referensi Game")]
    public DiceTotal diceTotal;
    public CameraFollow cameraFollow;
    public PionMovement[] daftarPion;
    [Header("Kartu Soal Otomatis")]
    public CardDisplayManager cardDisplayManager;
    [Header("Kartu EDUCARD")]
public KartuEdupolyManager kartuEdupolyManager;
public KartuEdupolyEffectManager kartuEffectManager;

    [Header("EDUCARD - Single Player vs Bot")]
    public KartuEdupolyEffectManager efekKartuEdupoly;
    [Tooltip("TMP Text untuk notifikasi singkat EDUCARD. Boleh dikosongkan, efek tetap berjalan.")]
    public TMP_Text teksHasilEducard;
    public float durasiNotifikasiEducard = 4f;

    [Header("Aturan Dadu Kembar")]
    public int jumlahKembarBerturut = 0;

    [Header("Auto Roll")]
    public float batasWaktuLempar = 15f;
    public TMP_Text textTimer;
    public Slider sliderTimer;

    private float waktuTersisa;
    private bool timerAktif = false;
    private bool giliranSedangDiproses = false;
    private bool permainanSelesaiBot = false;
    private readonly bool[] pemainBangkrutBot = new bool[4];

[Header("Hasil Akhir Lawan Bot")]
public TMP_Text teksHasilBot;
public GameObject tombolKembaliMenuBot;
    public bool ModeMultiplayer =>
        MainMenuManager.modeMultiplayer;

    void Start()
    {
        jumlahPemain = Mathf.Clamp(
            MainMenuManager.jumlahPemain, 2, 4
        );

        giliranSekarang = 0;
        jumlahKembarBerturut = 0;
        giliranSedangDiproses = false;

        if (daftarPion == null ||
            daftarPion.Length < jumlahPemain)
        {
            Debug.LogError(
                "Daftar pion belum lengkap!"
            );
            return;
        }

        // Aktifkan pion sesuai jumlah pemain
        for (int i = 0; i < daftarPion.Length; i++)
        {
            if (daftarPion[i] != null)
            {
                daftarPion[i].gameObject.SetActive(
                    i < jumlahPemain
                );
            }
        }

        TampilkanGiliran();
        StartCoroutine(RapikanPionAwal());

        // Giliran manusia dimulai dengan 15 detik
        if (!ModeMultiplayer)
        {
            if (!ModeMultiplayer && permainanSelesaiBot)
    return;
            MulaiTimerManusia();
        }
    }

    IEnumerator RapikanPionAwal()
    {
        yield return null;
        RapikanSemuaPion();
    }

    void Update()
    {
        if (ModeMultiplayer || permainanSelesaiBot)
    return;
        if (!timerAktif)
            return;

        if (!BolehLemparManusia())
            return;

        waktuTersisa -= Time.deltaTime;
        waktuTersisa = Mathf.Max(
            0f, waktuTersisa
        );

        PerbaruiTampilanTimer();

        // Waktu habis: dadu otomatis dikocok
        if (waktuTersisa <= 0f)
        {
            timerAktif = false;

            Debug.Log(
                "15 DETIK HABIS! AUTO ROLL!"
            );

            if (!CobaLemparManusia())
            {
                // Jika dadu belum siap,
                // beri kesempatan timer baru.
                MulaiTimerManusia();
            }
        }
    }

    void MulaiTimerManusia()
    {
        if (!ModeMultiplayer &&
            daftarPion != null &&
            daftarPion.Length > 0 &&
            daftarPion[0] != null &&
            daftarPion[0].sedangDipenjara)
        {
            if (!BayarDendaPenjara(0))
            {
                HentikanTimer();
                StartCoroutine(LewatiGiliranPenjara());
                return;
            }
        }
        waktuTersisa = Mathf.Max(
            1f, batasWaktuLempar
        );

        timerAktif = true;
        PerbaruiTampilanTimer();

        Debug.Log(
            "Player 1 punya " +
            waktuTersisa +
            " detik untuk melempar dadu."
        );
    }

    void HentikanTimer()
    {
        timerAktif = false;
        PerbaruiTampilanTimer();
    }

    void PerbaruiTampilanTimer()
    {
        if (textTimer != null)
        {
            if (timerAktif)
            {
                textTimer.text =
                    "Lempar Dadu: " +
                    Mathf.CeilToInt(waktuTersisa) +
                    " dtk";
            }
            else
            {
                textTimer.text = "";
            }
        }

        if (sliderTimer != null)
        {
            sliderTimer.value =
                Mathf.Clamp01(
                    waktuTersisa /
                    Mathf.Max(1f, batasWaktuLempar)
                );
        }
    }

    public bool BolehLemparManusia()
    {
        if (ModeMultiplayer || permainanSelesaiBot)
            return false;

        return giliranSekarang == 0 &&
               timerAktif &&
               !giliranSedangDiproses &&
               diceTotal != null &&
               !diceTotal.SedangRoll;
    }

    // Dipanggil tombol Space atau Auto Roll
    public bool CobaLemparManusia()
    {
        if (ModeMultiplayer)
            return false;
        if (!BolehLemparManusia())
        {
            // Khusus Auto Roll, timer mungkin
            // sudah dinonaktifkan pada detik 0.
            if (giliranSekarang != 0 ||
                giliranSedangDiproses ||
                diceTotal == null ||
                diceTotal.SedangRoll)
            {
                return false;
            }
        }

        giliranSedangDiproses = true;
        HentikanTimer();

        bool berhasil = diceTotal.LemparDadu();

        if (!berhasil)
        {
            giliranSedangDiproses = false;
            return false;
        }

        return true;
    }

    // Tetap tersedia untuk kompatibilitas
    public void MulaiProsesLemparan()
    {
        giliranSedangDiproses = true;
        HentikanTimer();
    }

    public void HasilLemparan(int total, bool kembar)
    {
        giliranSedangDiproses = true;

        StartCoroutine(
            ProsesHasilLemparan(total, kembar)
        );
    }

    IEnumerator ProsesHasilLemparan(
        int total,
        bool kembar
    )
    {
        if (daftarPion == null ||
            giliranSekarang < 0 ||
            giliranSekarang >= daftarPion.Length ||
            daftarPion[giliranSekarang] == null)
        {
            Debug.LogError(
                "Pion aktif tidak ditemukan!"
            );

            giliranSedangDiproses = false;
            yield break;
        }

        PionMovement pion =
            daftarPion[giliranSekarang];

        // Hitung dadu kembar
        if (kembar)
        {
            jumlahKembarBerturut++;

            Debug.Log(
                "Kembar ke-" +
                jumlahKembarBerturut
            );
        }
        else
        {
            jumlahKembarBerturut = 0;
        }

        // Tiga kali kembar: langsung penjara
        if (jumlahKembarBerturut >= 3)
        {
            Debug.Log(
                "KEMBAR 3 KALI! MASUK PENJARA!"
            );

            pion.MasukPenjara();

            yield return new WaitForSeconds(1f);

            GiliranBerikutnya();
            yield break;
        }

       // Gerakkan pion
Debug.Log(
    "Player " +
    (giliranSekarang + 1) +
    " maju " + total + " petak."
);

pion.Bergerak(total);

// Tunggu pion selesai berjalan.
yield return new WaitUntil(
    () => !pion.SedangBergerak
);

// ======================================
// PEMERIKSAAN MASUK PENJARA WP 27 KE WP 9
// ======================================

if (pion.sedangDipenjara && pion.posisiSekarang == 9)
{
    Debug.Log(
        "Player " + (giliranSekarang + 1) +
        " masuk penjara dari WP 27."
    );

    // Reset hitungan dadu kembar.
    jumlahKembarBerturut = 0;

    // Hentikan proses giliran saat ini.
    giliranSedangDiproses = false;

    // Lanjutkan ke pemain berikutnya.
    GiliranBerikutnya();

    // Hentikan proses petak lainnya.
    yield break;
}

// EDUCARD diproses hanya saat pemain/bot berhenti di petak kartu.
bool giliranTambahanDariKartu = false;
bool lewatiGiliranDariKartu = false;
        if (!ModeMultiplayer && efekKartuEdupoly != null &&
            efekKartuEdupoly.dekKartu != null &&
            efekKartuEdupoly.dekKartu.DatabaseSiap &&
            efekKartuEdupoly.dekKartu.ApakahPetakKartu(pion.posisiSekarang) && pion.putaranSelesai >= 1 )
        {
            Debug.Log("EDUCARD: pion mendarat di waypoint " + pion.posisiSekarang);
            yield return StartCoroutine(
                efekKartuEdupoly.ProsesKartu(giliranSekarang, pion)
            );

            giliranTambahanDariKartu = efekKartuEdupoly.mintaLemparUlang;
            lewatiGiliranDariKartu = efekKartuEdupoly.kehilanganGiliran;
        }
else if (!ModeMultiplayer && efekKartuEdupoly == null)
{
    Debug.LogWarning("EDUCARD belum diproses: field Efek Kartu Edupoly di TurnManager belum diisi.");
}

// FREE PARKING - WP 18
// Tidak memberikan hadiah atau hukuman.
if (!ModeMultiplayer && pion.posisiSekarang == 18)
{
    TampilkanNotifikasiEducard(
        "FREE PARKING\nKamu sedang beristirahat.\nTidak ada biaya yang harus dibayar."
    );

    Debug.Log(
        "FREE PARKING: Player " +
        (giliranSekarang + 1) +
        " berhenti di WP 18. Tidak ada perubahan saldo."
    );
}

// Cek apakah lahan SUDAH dimiliki pemain
// sebelum proses pembelian pada giliran ini.
bool bolehUpgradeSaatMendarat = false;

PropertyDataManager dataLahan =
    FindAnyObjectByType<PropertyDataManager>();

if (dataLahan != null && pion != null)
{
    DataLahan lahanAwal =
        dataLahan.CariLahan(pion.posisiSekarang);

    bolehUpgradeSaatMendarat =
        lahanAwal != null &&
        lahanAwal.pemilik == giliranSekarang &&
        lahanAwal.tahapProperti == 1;
}
// Proses transaksi satu kali setiap pendaratan.
ProsesPajak(pion); 
ProsesReward(pion);
ProsesSewaBot(pion);

if (permainanSelesaiBot ||
    pemainBangkrutBot[giliranSekarang])
{
    if (!permainanSelesaiBot)
        GiliranBerikutnya();

    yield break;
}
        // TAMBAHAN: Cek penawaran pembelian lahan
        yield return StartCoroutine(TungguPilihanPembelian(pion));

        
        // Pembelian otomatis khusus bot
        if (!ModeMultiplayer && giliranSekarang != 0)
        {
            yield return StartCoroutine(
                ProsesPembelianBot(pion)
            );
        }

        // Upgrade landmark di lahan sendiri.
if (!ModeMultiplayer && bolehUpgradeSaatMendarat)
{
    yield return StartCoroutine(
        PropertyUpgradeFlow.Proses(
            pion,
            giliranSekarang,
            ModeMultiplayer,
            cardDisplayManager
        )
    );
}

        
    // Pemeriksaan kemenangan terpusat:
// kuasai satu sisi atau tinggal satu pemain.
if (!ModeMultiplayer && CekPemenangBot())
{
    yield break;
}

        yield return new WaitForSeconds(1f);

        giliranSedangDiproses = false;

        if (lewatiGiliranDariKartu)
        {
            Debug.Log("EDUCARD: pemain kehilangan 1 giliran.");
            GiliranBerikutnya();
            yield break;
        }

        if (giliranTambahanDariKartu)
        {
            Debug.Log("EDUCARD: pemain mendapat kesempatan lempar dadu lagi.");
            if (giliranSekarang == 0) MulaiTimerManusia();
            else StartCoroutine(JalankanBot());
            yield break;
        }

        if (kembar)
        {
            Debug.Log(
                "Dadu kembar! Lempar lagi."
            );

            if (giliranSekarang == 0)
            {
                // Manusia mendapat 15 detik lagi
                MulaiTimerManusia();
            }
            else
            {
                // Bot otomatis melempar lagi
                StartCoroutine(JalankanBot());
            }
        }
        else
        {
            GiliranBerikutnya();
        }
    }

    
    private Coroutine notifikasiEducardCoroutine;

    private void TampilkanNotifikasiEducard(string pesan)
    {
        Debug.Log(pesan.Replace("\n", " | "));
        if (teksHasilEducard == null)
        {
            Debug.LogWarning("UI notifikasi EDUCARD belum dihubungkan ke field Teks Hasil Educard di TurnManager.");
            return;
        }
        if (notifikasiEducardCoroutine != null)
            StopCoroutine(notifikasiEducardCoroutine);
        notifikasiEducardCoroutine = StartCoroutine(
            NotifikasiEducardCoroutine(pesan)
        );
    }

    private IEnumerator NotifikasiEducardCoroutine(string pesan)
    {
        teksHasilEducard.gameObject.SetActive(true);
        teksHasilEducard.text = pesan;
        yield return new WaitForSeconds(durasiNotifikasiEducard);
        if (teksHasilEducard != null)
            teksHasilEducard.text = "";
        notifikasiEducardCoroutine = null;
    }

    public void GiliranBerikutnya()
    {
        if (!ModeMultiplayer)
        {
            // Tidak boleh berganti giliran setelah game selesai.
            if (permainanSelesaiBot || CekPemenangBot())
                return;

            int pemainBerikutnya = -1;

            // Cari pemain yang belum bangkrut.
            for (int langkah = 1;
                 langkah <= jumlahPemain;
                 langkah++)
            {
                int kandidat =
                    (giliranSekarang + langkah) % jumlahPemain;

                if (!pemainBangkrutBot[kandidat])
                {
                    pemainBerikutnya = kandidat;
                    break;
                }
            }

            if (pemainBerikutnya < 0)
                return;

            giliranSekarang = pemainBerikutnya;
        }
        else
        {
            // Jalur lama tetap tersedia.
            giliranSekarang =
                (giliranSekarang + 1) % jumlahPemain;
        }

        jumlahKembarBerturut = 0;
        giliranSedangDiproses = false;

        TampilkanGiliran();

        if (giliranSekarang == 0)
        {
            MulaiTimerManusia();
        }
        else
        {
            HentikanTimer();
            StartCoroutine(JalankanBot());
        }
    }


    IEnumerator JalankanBot()
    {
        if (ModeMultiplayer)
        {
            Debug.Log("Mode Multiplayer: Bot dinonaktifkan.");
            yield break;
        }
        Debug.Log(
            "BOT Player " +
            (giliranSekarang + 1) +
            " bersiap melempar..."
        );

        yield return new WaitForSeconds(2f);

        if (giliranSekarang == 0 ||
            giliranSedangDiproses)
        {
            yield break;
        }

        if (diceTotal == null)
        {
            Debug.LogError(
                "DiceTotal belum dihubungkan!"
            );
            yield break;
        }
        // Periksa penjara sebelum bot melempar dadu
        if (daftarPion != null &&
            giliranSekarang < daftarPion.Length &&
            daftarPion[giliranSekarang] != null &&
            daftarPion[giliranSekarang].sedangDipenjara)
        {
            if (!BayarDendaPenjara(giliranSekarang))
            {
                Debug.Log(
                    "Bot Player " + (giliranSekarang + 1) +
                    " tetap dipenjara dan melewati giliran."
                );

                GiliranBerikutnya();
                yield break;
            }
        }

        giliranSedangDiproses = true;

        bool berhasil = diceTotal.LemparDadu();

        if (!berhasil)
        {
            Debug.LogWarning(
                "Bot gagal melempar dadu."
            );

            giliranSedangDiproses = false;
        }

        // ProsesHasilLemparan yang mengatur
        // pergerakan dan giliran berikutnya.
    }

  void TampilkanGiliran()
{
    if (ModeMultiplayer)
    {
        Debug.Log(
            "Giliran PLAYER " +
            (giliranSekarang + 1) +
            " (MULTIPLAYER)"
        );
        return;
    }

    if (giliranSekarang == 0)
    {
        Debug.Log("Giliran PLAYER 1 (MANUSIA)");
    }
    else
    {
        Debug.Log(
            "Giliran PLAYER " +
            (giliranSekarang + 1) +
            " (BOT)"
        );
    }
}

    public void RapikanSemuaPion()
    {
        if (daftarPion == null)
            return;

        foreach (PionMovement pion in daftarPion)
        {
            if (pion != null &&
                pion.gameObject.activeInHierarchy &&
                !pion.SedangBergerak)
            {
                pion.UpdatePosisiDiam();
            }
        }
    }

    IEnumerator TungguPilihanPembelian(PionMovement pion)
    {
        // Tahap awal: khusus pemain manusia mode Lawan Bot.
        if (ModeMultiplayer || giliranSekarang != 0)
            yield break;

        if (pion == null || !pion.BolehBeliLahan)
            yield break;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PropertyPurchaseUI purchaseUI =
            FindAnyObjectByType<PropertyPurchaseUI>();
        SoalManager soalManager =
            FindAnyObjectByType<SoalManager>();
        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (dataManager == null || purchaseUI == null || soalManager == null || uang == null)
            yield break;

        // Ambil data sesuai petak tempat pion berhenti
        DataLahan lahan =
            dataManager.CariLahan(pion.posisiSekarang);

        // Bukan petak properti
        if (lahan == null)
            yield break;

        // Properti sudah dimiliki
        if (lahan.pemilik != -1)
            yield break;

        // Harga belum diisi
        if (lahan.harga <= 0)
        {
            Debug.Log(
                "Harga lahan " + lahan.namaLahan +
                " belum diisi."
            );
            yield break;
        }

        bool sudahMemilih = false;
        bool sedangMenjawab = false;
        bool sudahSelesaiMenjawab = false;

        System.Action aksiBeli = () =>
        {
            if (sudahMemilih)
                return;

            Debug.Log("Tombol Beli diklik: " + lahan.namaLahan);

            if (cardDisplayManager == null)
            {
                Debug.LogError("CardDisplayManager belum terhubung!");
                return;
            }

            bool berhasilTampil =
                cardDisplayManager.TampilkanKartuSesuaiPetak(lahan.namaLahan);

            Debug.Log("Kartu berhasil tampil: " + berhasilTampil);

            if (!berhasilTampil)
                return;

            sedangMenjawab = true;
            sudahMemilih = true;
        };
        System.Action aksiLewati = () =>
        {
            if (sudahMemilih)
                return;

            Debug.Log("Pemain melewati " + lahan.namaLahan);
            sudahMemilih = true;
        };

        System.Action<bool> hasilJawaban = (benar) =>
        {
            if (!sedangMenjawab || sudahSelesaiMenjawab)
                return;

            sudahSelesaiMenjawab = true;

            if (benar)
            {
                bool berhasil = dataManager.BeliLahan(
                    lahan, giliranSekarang, uang
                );

                Debug.Log(berhasil
                    ? "Pembelian berhasil!"
                    : "Pembelian gagal, saldo tidak cukup.");
            }
            else
            {
                Debug.Log("Jawaban salah, pembelian batal.");
            }
        };

        soalManager.OnJawabanSelesai += hasilJawaban;

        // Hubungkan keputusan pemain
        purchaseUI.saatKlikBeli += aksiBeli;
        purchaseUI.saatKlikLewati += aksiLewati;

        // Tampilkan nama, harga, dan warna sesuai lahan
        purchaseUI.TampilkanPembelian(
            lahan.namaLahan,
            lahan.harga,
            lahan.warnaKomplek
        );

        // Tunggu pemain memilih Beli atau Lewati
        yield return new WaitUntil(() => sudahMemilih);

        // Jika memilih Beli, tunggu sampai soal dijawab
        if (sedangMenjawab)
        {
            yield return new WaitUntil(() => sudahSelesaiMenjawab);

            // Tunggu pop-up hasil jawaban selesai
            yield return new WaitForSeconds(2f);
        }

        // Lepaskan event setelah proses selesai
        purchaseUI.saatKlikBeli -= aksiBeli;
        purchaseUI.saatKlikLewati -= aksiLewati;
        soalManager.OnJawabanSelesai -= hasilJawaban;
    }
    IEnumerator ProsesPembelianBot(PionMovement pion)
    {
        if (pion == null || !pion.BolehBeliLahan)
            yield break;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (dataManager == null || uang == null)
            yield break;

        DataLahan lahan = dataManager.CariLahan(pion.posisiSekarang);

        if (lahan == null || lahan.pemilik != -1 || lahan.harga <= 0)
            yield break;

        int idBot = giliranSekarang;

        if (uang.saldoPemain == null ||
            idBot < 0 ||
            idBot >= uang.saldoPemain.Length)
            yield break;

        // Bot hanya mencoba membeli jika saldo mencukupi
        if (uang.saldoPemain[idBot] < lahan.harga)
        {
            Debug.Log("Bot tidak cukup uang membeli " + lahan.namaLahan);
            yield break;
        }

        yield return new WaitForSeconds(1f);

        // Simulasi bot menjawab soal: peluang benar 70%
        bool jawabanBenar = Random.value < 0.75f;

        if (jawabanBenar)
        {
            bool berhasil = dataManager.BeliLahan(lahan, idBot, uang);

            Debug.Log(berhasil
                ? "Bot berhasil membeli " + lahan.namaLahan
                : "Bot gagal membeli " + lahan.namaLahan);
        }
        else
        {
            Debug.Log("Bot salah menjawab soal. Pembelian batal.");
        }
    }
    
    void ProsesSewaBot(PionMovement pion)
    {
        if (ModeMultiplayer || pion == null)
            return;

        PropertyDataManager data =
            FindAnyObjectByType<PropertyDataManager>();

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (data == null || uang == null ||
            uang.saldoPemain == null)
            return;

        DataLahan lahan =
            data.CariLahan(pion.posisiSekarang);

        if (lahan == null)
            return;

        int pembayar = giliranSekarang;
        int pemilik = lahan.pemilik;

        if (pemilik < 0 || pemilik == pembayar)
            return;

        if (pembayar < 0 ||
            pembayar >= uang.saldoPemain.Length ||
            pemilik >= uang.saldoPemain.Length)
            return;

        float persentase = 0f;

        if (lahan.tahapProperti == 1)
            persentase = 0.10f;
        else if (lahan.tahapProperti == 2)
            persentase = 0.20f;
        else if (lahan.tahapProperti == 3)
            persentase = 0.30f;

        int biayaSewa =
            Mathf.RoundToInt(lahan.harga * persentase);

        if (biayaSewa <= 0)
            return;

       if (!uang.KurangiSaldo(pembayar, biayaSewa))
{
    TandaiBangkrutBot(
        pembayar,
        "Tidak mampu membayar sewa Rp" +
        biayaSewa.ToString("N0")
    );

    return;
}

        uang.TambahSaldo(pemilik, biayaSewa);

        Debug.Log(
            "Player " + (pembayar + 1) +
            " membayar sewa Rp" +
            biayaSewa.ToString("N0") +
            " kepada Player " + (pemilik + 1)
        );
    }

    void ProsesPajak(PionMovement pion)
    {
        if (pion == null || ModeMultiplayer || pion.putaranSelesai < 1 )
            return;

        int biayaPajak = 0;

        if (pion.posisiSekarang == 5)
            biayaPajak = 100000;
        else if (pion.posisiSekarang == 33)
            biayaPajak = 150000;

        if (biayaPajak == 0)
            return;

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang == null)
            return;

        bool berhasil = uang.KurangiSaldo(
            giliranSekarang,
            biayaPajak
        );

        if (berhasil)
            Debug.Log("Player " + (giliranSekarang + 1) +
                      " membayar pajak Rp" + biayaPajak);
        else
    TandaiBangkrutBot(
        giliranSekarang,
        "Tidak mampu membayar pajak"
    );
    }
    bool BayarDendaPenjara(int idPemain)
    {
        if (ModeMultiplayer)
            return false;

        if (daftarPion == null ||
            idPemain < 0 ||
            idPemain >= daftarPion.Length)
            return false;

        PionMovement pion = daftarPion[idPemain];

        if (pion == null)
            return false;

        // Pemain yang tidak dipenjara boleh bermain biasa
        if (!pion.sedangDipenjara)
            return true;

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang == null)
            return false;

        const int denda = 100000;

        // Bayar denda hanya jika saldo mencukupi
        bool berhasil = uang.KurangiSaldo(idPemain, denda);

        if (!berhasil)
{
    Debug.LogWarning(
        "Player " + (idPemain + 1) +
        " belum mampu membayar denda penjara. Giliran dilewati."
    );

    return false;
}

        pion.sedangDipenjara = false;

        Debug.Log(
            "Player " + (idPemain + 1) +
            " membayar denda Rp100.000 dan BEBAS dari penjara!"
        );

        return true;
    }
    IEnumerator LewatiGiliranPenjara()
    {
        yield return new WaitForSeconds(1f);

        GiliranBerikutnya();
    }
    void ProsesReward(PionMovement pion)
    {
        if (pion == null || ModeMultiplayer || pion.putaranSelesai < 1 )
            return;

        // Reward hanya pada WP 11 dan WP 20
        if (pion.posisiSekarang != 11 &&
            pion.posisiSekarang != 20)
            return;

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang == null)
            return;

        int hadiah = 50000;

        uang.TambahSaldo(giliranSekarang, hadiah);

        Debug.Log(
            "Player " + (giliranSekarang + 1) +
            " mendapat REWARD Rp50.000!"
        );
    }
    
    private void TandaiBangkrutBot(int pemain, string alasan)
    {
        if (ModeMultiplayer || permainanSelesaiBot ||
            pemain < 0 || pemain >= jumlahPemain ||
            pemainBangkrutBot[pemain])
            return;

        pemainBangkrutBot[pemain] = true;

        PropertyDataManager data =
            FindAnyObjectByType<PropertyDataManager>();

        if (data != null)
        {
            for (int waypoint = 0; waypoint < 36; waypoint++)
            {
                DataLahan lahan = data.CariLahan(waypoint);

                if (lahan != null && lahan.pemilik == pemain)
                {
                    lahan.pemilik = -1;
                    lahan.tahapProperti = 0;
                }
            }
        }

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang != null &&
            uang.saldoPemain != null &&
            pemain < uang.saldoPemain.Length)
        {
            uang.saldoPemain[pemain] = 0;
            uang.PerbaruiUIPemain();
        }

        Debug.LogWarning(
            "PLAYER " + (pemain + 1) +
            " BANGKRUT! " + alasan
        );

        CekPemenangBot();
    }

    private bool CekPemenangBot()
    {
        if (ModeMultiplayer)
            return false;

        if (permainanSelesaiBot)
            return true;

        PropertyDataManager data =
            FindAnyObjectByType<PropertyDataManager>();

        if (data != null)
        {
            int pemenang =
                SideOwnershipWinChecker.CariPemenangSatuSisi(data);

            if (pemenang >= 0 &&
                pemenang < jumlahPemain &&
                !pemainBangkrutBot[pemenang])
            {
                AkhiriPermainanBot(
                    pemenang,
                    "Menguasai satu sisi papan"
                );
                return true;
            }
        }

        int jumlahAktif = 0;
        int pemainTerakhir = -1;

        for (int i = 0; i < jumlahPemain; i++)
        {
            if (!pemainBangkrutBot[i])
            {
                jumlahAktif++;
                pemainTerakhir = i;
            }
        }

        if (jumlahAktif == 1)
        {
            AkhiriPermainanBot(
                pemainTerakhir,
                "Semua lawan telah bangkrut"
            );
            return true;
        }

        if (pemainBangkrutBot[0])
        {
            AkhiriPermainanBot(-1, "Player 1 bangkrut");
            return true;
        }

        return false;
    }

private void AkhiriPermainanBot(int pemenang, string alasan)
{
    if (permainanSelesaiBot)
        return;

    permainanSelesaiBot = true;
    giliranSedangDiproses = false;
    HentikanTimer();

    string hasil =
        pemenang == 0
        ? "SELAMAT! KAMU MENANG!"
        : "KAMU KALAH!";

    string pesan =
        "EDU-POLY SELESAI!\n" +
        hasil + "\n" + alasan;

    Debug.Log(pesan);

        GameOverManager gameOver = FindAnyObjectByType<GameOverManager>();

        if (gameOver != null)
        {
            if (pemenang == 0)
                gameOver.TampilkanMenang();
            else
                gameOver.TampilkanKalah();
        }
        else
        {
            Debug.LogError("GameOverManager tidak ditemukan!");
        }
    }

    public void KembaliKeMenuBot()
    {
        if (ModeMultiplayer || !permainanSelesaiBot)
            return;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            "MainMenu"
        );
    }

}
