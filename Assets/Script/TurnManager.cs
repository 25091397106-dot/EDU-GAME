
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;

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

    [Header("Aturan Dadu Kembar")]
    public int jumlahKembarBerturut = 0;

    [Header("Auto Roll")]
    public float batasWaktuLempar = 15f;
    public TMP_Text textTimer;
    public Slider sliderTimer;

    private float waktuTersisa;
    private bool timerAktif = false;
    private bool giliranSedangDiproses = false;

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
        if (ModeMultiplayer)
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
        if (ModeMultiplayer)
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

        // Tunggu pion selesai berjalan
        yield return new WaitUntil(
            () => !pion.SedangBergerak
        );

        // TAMBAHAN: Cek penawaran pembelian lahan
        yield return StartCoroutine(TungguPilihanPembelian(pion));

        yield return new WaitForSeconds(1f);

        giliranSedangDiproses = false;

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

    public void GiliranBerikutnya()
    {
        giliranSekarang++;

        if (giliranSekarang >= jumlahPemain)
        {
            giliranSekarang = 0;
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
        if (giliranSekarang == 0)
        {
            Debug.Log(
                "Giliran PLAYER 1 (Manusia)"
            );
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

        if (dataManager == null || purchaseUI == null)
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

        System.Action aksiBeli = () =>
        {
            Debug.Log(
                "Pemain ingin membeli " +
                lahan.namaLahan +
                ". Selanjutnya perlu menjawab soal."
            );

            if (cardDisplayManager != null)
            {
                cardDisplayManager.TampilkanKartuSesuaiPetak(
                    lahan.namaLahan
                );
            }

            sudahMemilih = true;
        };

        System.Action aksiLewati = () =>
        {
            Debug.Log(
                "Pemain melewati " +
                lahan.namaLahan
            );

            sudahMemilih = true;
        };

        // Hubungkan keputusan pemain
        purchaseUI.saatKlikBeli += aksiBeli;
        purchaseUI.saatKlikLewati += aksiLewati;

        // Tampilkan nama, harga, dan warna sesuai lahan
        purchaseUI.TampilkanPembelian(
            lahan.namaLahan,
            lahan.harga,
            lahan.warnaKomplek
        );

        // Giliran menunggu pemain memilih
        yield return new WaitUntil(
            () => sudahMemilih
        );

        // Lepaskan event agar tidak menumpuk
        purchaseUI.saatKlikBeli -= aksiBeli;
        purchaseUI.saatKlikLewati -= aksiLewati;
    }
}
