
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class NetworkGameManager : NetworkBehaviour
{
    
    [Header("UI Kartu Giliran")]
    public TMPro.TextMeshProUGUI textHasilKartu;
    [Header("EDUCARD Multiplayer")]
public KartuEdupolyManager pengelolaDekEdupoly;

private bool educardMintaLemparUlang;
private bool educardKehilanganGiliran;
private bool sedangTampilEduCardLokal = false;
private Coroutine educardDisplayCoroutine;

// Status kuis EDUCARD Multiplayer
private bool educardKuisSelesaiOnline;
private bool educardKuisBenarOnline;
private ulong educardClientKuisAktifOnline = ulong.MaxValue;
    [Header("Tombol Setelah Menang")]
public GameObject tombolKembaliMenu;
    [Header("Identitas Pemain")]
    public int playerIndexLokal = -1;
    public int jumlahPemainOnline = 0;

    [Header("Status Permainan Online")]
    public NetworkVariable<int> giliranOnline =
        new NetworkVariable<int>(0);

    public NetworkVariable<int> waktuOnline =
        new NetworkVariable<int>(15);

    public NetworkVariable<bool> sedangProsesOnline =
        new NetworkVariable<bool>(false);
// ==========================================
// STATUS AKHIR PERMAINAN MULTIPLAYER
// ==========================================
public NetworkVariable<bool> permainanSelesaiOnline =
    new NetworkVariable<bool>(false);

    // Slot tidak boleh bergeser setelah pertandingan dimulai.
    private NetworkVariable<bool> pemainOnlineTerkunci =
        new NetworkVariable<bool>(false);

    // Menyimpan ID asli masing-masing pemain
    private NetworkVariable<ulong> slot1 =
        new NetworkVariable<ulong>(ulong.MaxValue);

    private NetworkVariable<ulong> slot2 =
        new NetworkVariable<ulong>(ulong.MaxValue);

    private NetworkVariable<ulong> slot3 =
        new NetworkVariable<ulong>(ulong.MaxValue);

    private NetworkVariable<ulong> slot4 =
        new NetworkVariable<ulong>(ulong.MaxValue);

    private float sisaWaktuHost = 15f;

    // Ditetapkan Host hanya setelah event Netcode menyatakan
    // semua peserta selesai memuat scene permainan.
    private bool seluruhClientSiapScene = false;

   
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback +=
                SaatPemainTerhubungOnline;

            NetworkManager.OnClientDisconnectCallback +=
                SaatPemainTerputusOnline;

            // Scene game baru dimulai dalam status menunggu.
            // Jangan kunci slot di sini: belum tentu semua client siap.
            seluruhClientSiapScene = false;
            pemainOnlineTerkunci.Value = false;
            giliranOnline.Value = 0;
            waktuOnline.Value = 15;
            sedangProsesOnline.Value = false;
            permainanSelesaiOnline.Value = false;
            statusTerputusOnline.Value = 0;
            statusBangkrutOnline.Value = 0;
            sisaWaktuHost = 15f;

            AturIdentitasPemain();

            if (NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneEvent +=
                    SaatEventSceneOnline;
            }
            else
            {
                Debug.LogError(
                    "MULTIPLAYER: Netcode SceneManager tidak tersedia. " +
                    "Pertandingan tetap menunggu agar tidak mulai terlalu dini."
                );
            }
        }

        PerbaruiIdentitasLokal();
        Debug.Log("NetworkGameManager aktif; menunggu sinkronisasi scene.");
    }

    // Hanya event ini yang menandai bahwa pemuatan scene
    // telah selesai di Host dan seluruh Client.
    private void SaatEventSceneOnline(SceneEvent sceneEvent)
    {
        if (!IsServer || pemainOnlineTerkunci.Value)
            return;

        if (sceneEvent.SceneEventType !=
            SceneEventType.LoadEventCompleted)
            return;

        if (sceneEvent.SceneName != "game")
            return;

        if (sceneEvent.ClientsThatTimedOut != null &&
            sceneEvent.ClientsThatTimedOut.Count > 0)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Ada client gagal memuat scene game. " +
                "Pertandingan belum dimulai."
            );
            return;
        }

        // Periksa setiap client yang terhubung, bukan sekadar
        // menghitung jumlah slot. Termasuk Host.
        foreach (ulong id in NetworkManager.ConnectedClientsIds)
        {
            if (sceneEvent.ClientsThatCompleted == null ||
                !sceneEvent.ClientsThatCompleted.Contains(id))
            {
                Debug.LogWarning(
                    "MULTIPLAYER: Client " + id +
                    " belum selesai memuat scene game."
                );
                return;
            }
        }

        seluruhClientSiapScene = true;
        AturIdentitasPemain();
        KunciPemainOnline();
    }

    // ==========================================
    // PEMAIN BARU BERGABUNG MULTIPLAYER
    // ==========================================
    private void SaatPemainTerhubungOnline(ulong clientId)
    {
        if (!IsServer)
            return;

        // Perbarui daftar ID pemain di Host.
        AturIdentitasPemain();

        Debug.Log(
            "MULTIPLAYER: Client " + clientId +
            " terhubung. Jumlah pemain: " +
            jumlahPemainOnline
        );
    }

    // ==========================================
    // DETEKSI PEMAIN TERPUTUS MULTIPLAYER
    // ==========================================
   
    // ==========================================
    // PEMAIN MULTIPLAYER TERPUTUS
    // ==========================================
    private void SaatPemainTerputusOnline(ulong clientId)
    {
        if (!IsServer)
            return;

        int pemainTerputus = -1;

        // Cari nomor pemain berdasarkan slot tetap.
        if (clientId == slot1.Value)
            pemainTerputus = 0;
        else if (clientId == slot2.Value)
            pemainTerputus = 1;
        else if (clientId == slot3.Value)
            pemainTerputus = 2;
        else if (clientId == slot4.Value)
            pemainTerputus = 3;

        if (pemainTerputus < 0)
            return;

        // Disconnect setelah pertandingan dimulai dianggap MENYERAH.
        int penanda = 1 << pemainTerputus;
        statusTerputusOnline.Value |= penanda;

        if (pemainOnlineTerkunci.Value && !permainanSelesaiOnline.Value)
        {
            statusBangkrutOnline.Value |= penanda;
            LepaskanAsetPemainBangkrutRpc(pemainTerputus);

            Debug.LogWarning(
                "MULTIPLAYER: Player " + (pemainTerputus + 1) +
                " TERPUTUS DAN DIANGGAP MENYERAH. Aset dikembalikan ke bank."
            );

            if (pemainTerputus == giliranOnline.Value)
            {
                bool sedangMenungguPilihan =
                    sedangPembelianOnline || sedangUpgradeOnline ||
                    sedangAmbilAlihOnline;

                if (sedangPembelianOnline)
                {
                    sedangPembelianOnline = false;
                    pemainPembeliOnline = -1;
                    waypointPembelianOnline = -1;
                }

                if (sedangUpgradeOnline)
                {
                    sedangUpgradeOnline = false;
                    pemainUpgradeOnline = -1;
                    waypointUpgradeOnline = -1;
                }

                if (sedangAmbilAlihOnline)
                {
                    sedangAmbilAlihOnline = false;
                    pemainPenantangOnline = -1;
                    waypointAmbilAlihOnline = -1;
                    pemilikLamaOnline = -1;
                }

                if (sedangMenungguPilihan)
                    TutupPembelianOnlineRpc();
                else if (!sedangProsesOnline.Value)
                {
                    jumlahKembarOnline = 0;
                    GiliranOnlineBerikutnya();
                }
                // Coroutine lemparan yang sedang berjalan akan
                // mendeteksi status terputus dan melewati pemain ini.
            }

            CekKemenanganPemainAktifOnline();
        }
        else
        {
            Debug.LogWarning(
                "MULTIPLAYER: Player " + (pemainTerputus + 1) +
                " terputus sebelum pertandingan dimulai."
            );
        }
    }


    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -=
                SaatPemainTerhubungOnline;

            NetworkManager.OnClientDisconnectCallback -=
                SaatPemainTerputusOnline;

            if (IsServer && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneEvent -=
                    SaatEventSceneOnline;
            }
        }
    }


    private void PerbaruiIdentitasLokal()
    {
        if (!IsSpawned)
            return;

        ulong id = NetworkManager.LocalClientId;

        if (id == slot1.Value)
            playerIndexLokal = 0;
        else if (id == slot2.Value)
            playerIndexLokal = 1;
        else if (id == slot3.Value)
            playerIndexLokal = 2;
        else if (id == slot4.Value)
            playerIndexLokal = 3;
        else
            playerIndexLokal = -1;

        jumlahPemainOnline = 0;

        if (slot1.Value != ulong.MaxValue)
            jumlahPemainOnline++;

        if (slot2.Value != ulong.MaxValue)
            jumlahPemainOnline++;

        if (slot3.Value != ulong.MaxValue)
            jumlahPemainOnline++;

        if (slot4.Value != ulong.MaxValue)
            jumlahPemainOnline++;
    }

    void Update()
    {
        if (!IsSpawned)
            return;
        
        
        // Tampilkan kartu selama 8 detik sejak
        // data kartu pertama kali diterima.
       if (!sedangTampilEduCardLokal &&
    !permainanSelesaiOnline.Value &&
    kartuPlayer1.Value > 0)
        {
            if (!hasilKartuSudahDimulai)
            {
                hasilKartuSudahDimulai = true;
                sisaTampilKartu = 8f;
            }

            if (sisaTampilKartu > 0f)
            {
                PerbaruiTeksKartuOnline();
                sisaTampilKartu -= Time.deltaTime;
            }
            else if (textHasilKartu != null)
            {
                textHasilKartu.text = "";
            }
        }


        PerbaruiIdentitasLokal();

        TurnManager turnManager = FindAnyObjectByType<TurnManager>();
        if (turnManager != null && turnManager.textTimer != null)
        {
            // Timer hanya tampil setelah semua client siap,
            // hanya pada layar pemain yang sedang mendapat giliran.
            if (pemainOnlineTerkunci.Value &&
                !permainanSelesaiOnline.Value &&
                playerIndexLokal == giliranOnline.Value)
            {
                turnManager.textTimer.text = waktuOnline.Value.ToString();
            }
            else
            {
                turnManager.textTimer.text = "";
            }
        }

        if (!IsServer || !pemainOnlineTerkunci.Value ||
            permainanSelesaiOnline.Value || sedangProsesOnline.Value)
            return;

        sisaWaktuHost -= Time.deltaTime;
        sisaWaktuHost = Mathf.Max(0f, sisaWaktuHost);

        int detik = Mathf.CeilToInt(sisaWaktuHost);
        if (waktuOnline.Value != detik)
            waktuOnline.Value = detik;

        if (sisaWaktuHost <= 0f)
        {
            sedangProsesOnline.Value = true;
            Debug.Log("AUTO ROLL MULTIPLAYER!");
            StartCoroutine(ProsesLemparanOnline());
        }
    }

    // Dipanggil pemain yang menekan Space
    public void MintaLemparDadu()
    {
        if (!IsSpawned || !IsClient)
            return;
        // Dadu belum boleh dilempar sebelum lobby siap.
        if (!pemainOnlineTerkunci.Value)
            return;
        if (permainanSelesaiOnline.Value)
             return;
        if (playerIndexLokal != giliranOnline.Value)
            return;

        if (sedangProsesOnline.Value)
            return;

        MintaLemparDaduRpc();
    }

    [Rpc(SendTo.Server)]
    private void MintaLemparDaduRpc(
        RpcParams rpcParams = default)
    {
        if (!IsServer ||
    !pemainOnlineTerkunci.Value ||
    permainanSelesaiOnline.Value)
{
    return;
}

        ulong pengirim =
            rpcParams.Receive.SenderClientId;

        int indexPengirim = -1;

        if (pengirim == slot1.Value)
            indexPengirim = 0;
        else if (pengirim == slot2.Value)
            indexPengirim = 1;
        else if (pengirim == slot3.Value)
            indexPengirim = 2;
        else if (pengirim == slot4.Value)
            indexPengirim = 3;

        if (indexPengirim != giliranOnline.Value)
        {
            Debug.LogWarning(
                "Permintaan ditolak: bukan giliran pemain."
            );
            return;
        }
       
        if (sedangProsesOnline.Value)
            return;

        sedangProsesOnline.Value = true;

        Debug.Log(
            "HOST menerima lemparan dari PLAYER " +
            (indexPengirim + 1)
        );
        StartCoroutine(ProsesLemparanOnline());

        // Hasil dadu dan gerakan pion akan
        // diintegrasikan pada tahap selanjutnya.
    }

    // Dipanggil Host saat giliran selesai
    
    
    // ==========================================
    // IDENTITAS TETAP PLAYER 1-4
    // ==========================================
    private void AturIdentitasPemain()
    {
        if (!IsServer || NetworkManager == null)
            return;

        if (pemainOnlineTerkunci.Value)
            return;

        List<ulong> ids = new List<ulong>(
            NetworkManager.ConnectedClientsIds
        );

        // Host ditempatkan pertama saat awal.
        ids.Remove(NetworkManager.ServerClientId);
        ids.Sort();
        ids.Insert(0, NetworkManager.ServerClientId);

        foreach (ulong id in ids)
        {
            // Jangan daftarkan ulang pemain
            // yang sudah memiliki slot.
            if (id == slot1.Value ||
                id == slot2.Value ||
                id == slot3.Value ||
                id == slot4.Value)
            {
                continue;
            }

            // Isi hanya slot yang belum pernah
            // digunakan. Slot lama tidak digeser.
            if (slot1.Value == ulong.MaxValue)
            {
                slot1.Value = id;
            }
            else if (slot2.Value == ulong.MaxValue)
            {
                slot2.Value = id;
            }
            else if (slot3.Value == ulong.MaxValue)
            {
                slot3.Value = id;
            }
            else if (slot4.Value == ulong.MaxValue)
            {
                slot4.Value = id;
            }
            else
            {
                Debug.LogWarning(
                    "MULTIPLAYER: Semua slot pemain " +
                    "sudah terisi. Client " + id +
                    " tidak mendapat slot."
                );
            }
        }

        // Jumlah slot yang sudah terdaftar,
        // termasuk pemain yang sedang terputus.
        jumlahPemainOnline = 0;

        if (slot1.Value != ulong.MaxValue)
            jumlahPemainOnline++;

        if (slot2.Value != ulong.MaxValue)
            jumlahPemainOnline++;

        if (slot3.Value != ulong.MaxValue)
            jumlahPemainOnline++;

        if (slot4.Value != ulong.MaxValue)
            jumlahPemainOnline++;

        Debug.Log(
            "MULTIPLAYER: Total slot terdaftar = " +
            jumlahPemainOnline
        );
    }

        
        
    // Dipanggil oleh Host saat lobby siap memulai pertandingan.
    public void KunciPemainOnline()
    {
        if (!IsServer || pemainOnlineTerkunci.Value)
            return;

        if (!seluruhClientSiapScene)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Menunggu semua client memuat scene game."
            );
            return;
        }

        AturIdentitasPemain();
        
        // Jumlah pemain harus sesuai kapasitas room.
        int jumlahDibutuhkan =
            Mathf.Clamp(MainMenuManager.jumlahPemain, 2, 4);

        if (jumlahPemainOnline < jumlahDibutuhkan)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Menunggu pemain terdaftar. " +
                jumlahPemainOnline + "/" + jumlahDibutuhkan
            );
            return;
        }


        // Tolak memulai jika jumlah koneksi aktif berkurang,
        // sekalipun slot pemain lama masih tersimpan.
        if (NetworkManager.ConnectedClientsIds.Count < jumlahDibutuhkan ||
            statusTerputusOnline.Value != 0)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Jumlah client aktif belum lengkap."
            );
            return;
        }

        pemainOnlineTerkunci.Value = true;
        
        // Acak urutan pemain menggunakan kartu.
        urutanKartuOnline =
            KartuUrutanGiliran.AcakUrutan(jumlahPemainOnline);
        
        // Buat angka kartu dari urutan hasil pengacakan.
        int[] angkaKartu =
            KartuUrutanGiliran.BuatAngkaKartu(urutanKartuOnline);

        // Host membagikan angka kartu ke semua pemain.
        kartuPlayer1.Value = angkaKartu.Length > 0 ? angkaKartu[0] : 0;
        kartuPlayer2.Value = angkaKartu.Length > 1 ? angkaKartu[1] : 0;
        kartuPlayer3.Value = angkaKartu.Length > 2 ? angkaKartu[2] : 0;
        kartuPlayer4.Value = angkaKartu.Length > 3 ? angkaKartu[3] : 0;

        Debug.Log(
            "KARTU MULTIPLAYER: " +
            "P1=" + kartuPlayer1.Value +
            ", P2=" + kartuPlayer2.Value +
            ", P3=" + kartuPlayer3.Value +
            ", P4=" + kartuPlayer4.Value
        );

        // Tentukan pemain yang mendapat giliran pertama.
        giliranOnline.Value = urutanKartuOnline[0];
        // Simpan pemain pertama secara permanen.
pemainPertamaOnline.Value = urutanKartuOnline[0];
        TampilkanHasilKartuOnline();
        jumlahKembarOnline = 0;

        // Mulai timer giliran pertama.
        ResetTimerOnline();

        Debug.Log(
            "KARTU GILIRAN: Player " +
            (giliranOnline.Value + 1) +
            " mendapat giliran pertama."
        );

        sisaWaktuHost = 15f;
        waktuOnline.Value = 15;
        sedangProsesOnline.Value = false;
        Debug.Log("MULTIPLAYER: Semua client siap, slot dikunci dan timer dimulai.");
    }

    // Host memindahkan giliran ke pemain berikutnya.
    public void GiliranOnlineBerikutnya()
    {
        if (!IsServer || permainanSelesaiOnline.Value)
            return;

        int jumlah = Mathf.Clamp(jumlahPemainOnline, 1, 4);
        int pemainSekarang = giliranOnline.Value;
        int pemainBerikutnya = -1;
        int jumlahAktif = 0;

        // Disconnect bukan bangkrut: hitung semua pemain yang belum bangkrut.
        for (int i = 0; i < jumlah; i++)
        {
            if (!ApakahPemainBangkrutOnline(i) &&
                !ApakahPemainTerputusOnline(i))
                jumlahAktif++;
        }

        // Ikuti urutan kartu hasil pengacakan Host.
        if (urutanKartuOnline == null ||
            urutanKartuOnline.Length == 0)
        {
            Debug.LogWarning("Urutan kartu belum tersedia!");
            return;
        }

        int posisiSekarang = System.Array.IndexOf(
            urutanKartuOnline,
            pemainSekarang
        );

        if (posisiSekarang < 0)
        {
            Debug.LogWarning(
                "Pemain tidak ditemukan dalam urutan kartu!"
            );
            return;
        }

        for (int langkah = 1;
             langkah < urutanKartuOnline.Length;
             langkah++)
        {
            int posisiBerikutnya =
                (posisiSekarang + langkah) %
                urutanKartuOnline.Length;

            int kandidat =
                urutanKartuOnline[posisiBerikutnya];

            if (!ApakahPemainBangkrutOnline(kandidat) &&
                !ApakahPemainTerputusOnline(kandidat))
            {
                pemainBerikutnya = kandidat;
                break;
            }
        }

        // ==========================================
        // KEMENANGAN KARENA LAWAN BANGKRUT
        // ==========================================
        if (jumlahAktif <= 1)
        {
            // Jangan mengumumkan pemenang dua kali.
            if (permainanSelesaiOnline.Value)
                return;

            int pemainMenang = -1;

            for (int i = 0; i < jumlah; i++)
            {
                if (!ApakahPemainBangkrutOnline(i) &&
                    !ApakahPemainTerputusOnline(i))
                {
                    pemainMenang = i;
                    break;
                }
            }

            // Hentikan pertandingan secara permanen.
            permainanSelesaiOnline.Value = true;
            sedangProsesOnline.Value = true;
            waktuOnline.Value = 0;
            sisaWaktuHost = 0f;

            if (pemainMenang >= 0)
            {
                Debug.Log(
                    "EDU-POLY MULTIPLAYER SELESAI!\n" +
                    "PLAYER " + (pemainMenang + 1) +
                    " MENANG!\n" +
                    "Semua lawan telah bangkrut."
                );

                // Umumkan pemenang ke semua perangkat.
                UmumkanPemenangOnlineRpc(
                    pemainMenang
                );
            }
            else
            {
                Debug.LogWarning(
                    "Permainan selesai tanpa pemenang aktif."
                );
            }

            return;
        }
        if (pemainBerikutnya == -1)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Tidak ada pemain " +
                "yang terhubung dan bisa mendapat giliran."
            );

            // Hentikan sementara timer.
            // Jangan nyatakan pertandingan selesai,
            // karena disconnect bukan bangkrut.
            sedangProsesOnline.Value = true;
            waktuOnline.Value = 0;
            sisaWaktuHost = 0f;

            return;
        }


        giliranOnline.Value = pemainBerikutnya;

        jumlahKembarOnline = 0;

        ResetTimerOnline();

        Debug.Log(
            "MULTIPLAYER: Giliran berpindah " +
            "ke Player " + (pemainBerikutnya + 1)
        );
    }


   
    public void ResetTimerOnline()
    {
        if (!IsServer)
            return;

        // Jangan aktifkan timer jika pertandingan selesai.
        if (permainanSelesaiOnline.Value)
        {
            sisaWaktuHost = 0f;
            waktuOnline.Value = 0;
            sedangProsesOnline.Value = true;
            return;
        }

        sisaWaktuHost = 15f;
        waktuOnline.Value = 15;
        sedangProsesOnline.Value = false;
    }


    // ==========================================
    // STATUS BANGKRUT PEMAIN MULTIPLAYER
    // ==========================================

    // Setiap bit menyimpan status satu pemain.
    // Data disinkronkan otomatis dari Host.
    private NetworkVariable<int> statusBangkrutOnline =
        new NetworkVariable<int>(0);
    // Status pemain yang terputus dari Multiplayer.
// Setiap bit mewakili satu Player (0 sampai 3).
private NetworkVariable<int> statusTerputusOnline =
    new NetworkVariable<int>(0);

// Memeriksa apakah pemain sedang terputus.
private bool ApakahPemainTerputusOnline(int pemain)
{
    if (pemain < 0 || pemain >= 4)
        return false;

    int penanda = 1 << pemain;

    return (statusTerputusOnline.Value & penanda) != 0;
}
    // Memeriksa apakah pemain sudah bangkrut.
    private bool ApakahPemainBangkrutOnline(int pemain)
    {
        if (pemain < 0 || pemain >= 4)
            return false;

        int penanda = 1 << pemain;

        return (statusBangkrutOnline.Value & penanda) != 0;
    }

 // Hanya Host yang boleh menyatakan bangkrut.
private void TandaiBangkrutOnline(int pemain)
{
    if (!IsServer ||
        permainanSelesaiOnline.Value ||
        pemain < 0 ||
        pemain >= jumlahPemainOnline)
    {
        return;
    }

    if (ApakahPemainBangkrutOnline(pemain))
        return;

    int penanda = 1 << pemain;

    statusBangkrutOnline.Value |= penanda;

    // Atur saldo pemain bangkrut menjadi Rp0.
    PlayerMoneyManager uang =
        FindAnyObjectByType<PlayerMoneyManager>();

    if (uang != null &&
        uang.saldoPemain != null &&
        pemain < uang.saldoPemain.Length)
    {
        uang.saldoPemain[pemain] = 0;

        // Sinkronkan saldo Rp0 ke seluruh perangkat.
        SinkronkanSaldoOnlineRpc(pemain, 0);
    }
    else
    {
        Debug.LogWarning(
            "MULTIPLAYER: Data saldo Player " +
            (pemain + 1) + " tidak ditemukan."
        );
    }

    // Kembalikan seluruh aset pemain ke bank.
    LepaskanAsetPemainBangkrutRpc(pemain);

    Debug.Log(
        "MULTIPLAYER: Player " +
        (pemain + 1) +
        " DINYATAKAN BANGKRUT! Saldo menjadi Rp0 dan semua aset kembali ke bank."
    );
}

    // Dipanggil oleh Host setelah status bangkrut tercatat.
    // Untuk setiap lahan, pemilik dan tahap bangunan harus
    // diperbarui bersama agar tidak meninggalkan landmark lama.
    [Rpc(SendTo.Everyone)]
    private void LepaskanAsetPemainBangkrutRpc(int pemain)
    {
        if (pemain < 0 || pemain > 3)
            return;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        if (dataManager == null)
        {
            Debug.LogError(
                "MULTIPLAYER: Tidak dapat melepas aset bangkrut; " +
                "PropertyDataManager tidak ditemukan."
            );
            return;
        }

        int jumlahDilepas = 0;

        // Papan EDU-POLY memiliki waypoint 0 sampai 35.
        for (int waypoint = 0; waypoint < 36; waypoint++)
        {
            DataLahan lahan = dataManager.CariLahan(waypoint);
            if (lahan == null || lahan.pemilik != pemain)
                continue;

            lahan.pemilik = -1;
            lahan.tahapProperti = 0;
            jumlahDilepas++;
        }

        Debug.Log(
            "MULTIPLAYER: Aset Player " + (pemain + 1) +
            " dilepas ke bank. Total lahan: " + jumlahDilepas
        );
    }
    
    // Urutan giliran dari hasil pengacakan kartu.
    private int[] urutanKartuOnline;
    // Pemain pertama berdasarkan hasil kartu.
// Disinkronkan ke Host dan semua Client.
private NetworkVariable<int> pemainPertamaOnline =
    new NetworkVariable<int>(-1);
    
    private float sisaTampilKartu = 0f;
private bool hasilKartuSudahDimulai = false;
    // Angka kartu setiap pemain, disinkronkan Host.
    // Nilai 0 berarti kartu belum dibagikan.
    private NetworkVariable<int> kartuPlayer1 =
        new NetworkVariable<int>(0);

    private NetworkVariable<int> kartuPlayer2 =
        new NetworkVariable<int>(0);

    private NetworkVariable<int> kartuPlayer3 =
        new NetworkVariable<int>(0);

    private NetworkVariable<int> kartuPlayer4 =
        new NetworkVariable<int>(0);
    
    // Membaca angka kartu yang sudah dibagikan Host.
    public int AmbilAngkaKartuOnline(int pemain)
    {
        if (!IsSpawned)
            return 0;

        switch (pemain)
        {
            case 0: return kartuPlayer1.Value;
            case 1: return kartuPlayer2.Value;
            case 2: return kartuPlayer3.Value;
            case 3: return kartuPlayer4.Value;
            default: return 0;
        }
    }
    
    // Menampilkan hasil kartu urutan giliran.
    public void TampilkanHasilKartuOnline()
    {
        if (!IsSpawned)
            return;

        string hasil = "HASIL KARTU EDU-POLY\n";

        for (int i = 0; i < jumlahPemainOnline; i++)
        {
            hasil += "Player " + (i + 1) +
                     ": Kartu " +
                     AmbilAngkaKartuOnline(i) + "\n";
        }

        hasil += "Giliran pertama: Player " +
                 (pemainPertamaOnline.Value + 1);

        Debug.Log(hasil);
        
        if (textHasilKartu != null)
        {
            textHasilKartu.text = hasil;
        }

    }
        
    // Perbarui teks kartu pada Host dan Client.
    private void PerbaruiTeksKartuOnline()
    {
        if (textHasilKartu == null ||
            !IsSpawned)
            return;

        // Jangan tampilkan sebelum kartu dibagikan.
        if (kartuPlayer1.Value == 0)
        {
            textHasilKartu.text = "";
            return;
        }

        string hasil = "HASIL KARTU EDU-POLY\n";

        for (int i = 0; i < jumlahPemainOnline; i++)
        {
            hasil += "Player " + (i + 1) +
                     ": Kartu " +
                     AmbilAngkaKartuOnline(i) + "\n";
        }

        hasil += "Giliran pertama: Player " +
         (pemainPertamaOnline.Value + 1);

        textHasilKartu.text = hasil;
    }

    private int jumlahKembarOnline = 0;
    // Menandai proses pembelian lahan multiplayer
    private bool sedangPembelianOnline = false;
    private int pemainPembeliOnline = -1;
    private int waypointPembelianOnline = -1;

    // =====================================
    // STATUS AMBIL ALIH LAHAN MULTIPLAYER
    // =====================================
    private bool sedangAmbilAlihOnline = false;
    private int pemainPenantangOnline = -1;
    private int waypointAmbilAlihOnline = -1;
    private int pemilikLamaOnline = -1;

    // =====================================
    // STATUS UPGRADE LANDMARK MULTIPLAYER
    // =====================================
    private bool sedangUpgradeOnline = false;
    private int pemainUpgradeOnline = -1;
    private int waypointUpgradeOnline = -1;
// Menyimpan callback soal Multiplayer yang aktif.
private System.Action<bool> callbackSoalOnline;
   
    private IEnumerator ProsesLemparanOnline()
    {
        // Hanya Host yang mengatur proses permainan.
        if (!IsServer || !pemainOnlineTerkunci.Value)
            yield break;

        // Jangan mulai lemparan jika sudah ada pemenang.
        if (permainanSelesaiOnline.Value)
            yield break;

        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        DiceTotal dadu =
            FindAnyObjectByType<DiceTotal>();

        if (turnManager == null ||
            dadu == null ||
            dadu.diceMix == null ||
            turnManager.daftarPion == null)
        {
            Debug.LogError(
                "Referensi gameplay multiplayer belum lengkap!"
            );

            ResetTimerOnline();
            yield break;
        }

        int pemain = giliranOnline.Value;
        educardMintaLemparUlang = false;
        educardKehilanganGiliran = false;

        if (pemain < 0 ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null)
        {
            Debug.LogError("Pion multiplayer tidak ditemukan!");
            ResetTimerOnline();
            yield break;
        }

        // Periksa penjara sebelum melempar dadu
        if (!BayarDendaPenjaraOnline(pemain))
        {
            Debug.Log(
                "MULTIPLAYER: Player " + (pemain + 1) +
                " melewati giliran karena belum membayar denda."
            );

            jumlahKembarOnline = 0;
            GiliranOnlineBerikutnya();
            yield break;
        }

        // Host menentukan satu hasil untuk semua pemain
        int angka1 = Random.Range(1, 7);
        int angka2 = Random.Range(1, 7);

        bool kembar = angka1 == angka2;

        if (kembar)
            jumlahKembarOnline++;
        else
            jumlahKembarOnline = 0;

        bool masukPenjara = jumlahKembarOnline >= 3;

        Debug.Log(
            "PLAYER " + (pemain + 1) +
            " mendapat " + angka1 + " + " + angka2
        );

        // Catat posisi awal sebelum RPC menggerakkan pion.
        // Host harus menunggu pergerakan BENAR-BENAR dimulai,
        // bukan langsung menganggap pion selesai bergerak.
        int posisiAwal = turnManager.daftarPion[pemain].posisiSekarang;

        // Semua perangkat menjalankan hasil yang sama
        TampilkanLemparanRpc(
            pemain,
            angka1,
            angka2,
            masukPenjara
        );

        // Tunggu animasi pengocokan selesai
        yield return new WaitForSeconds(
            dadu.diceMix.durasi + 0.2f
        );

        if (!masukPenjara)
        {
            PionMovement pion = turnManager.daftarPion[pemain];

            // Tunggu gerakan benar-benar dimulai. Status SedangBergerak
            // masih mungkin false saat animasi dadu baru selesai.
            float batasMulaiGerak = 10f;
            while (!pion.SedangBergerak &&
                   pion.posisiSekarang == posisiAwal &&
                   batasMulaiGerak > 0f)
            {
                batasMulaiGerak -= Time.deltaTime;
                yield return null;
            }

            if (!pion.SedangBergerak &&
                pion.posisiSekarang == posisiAwal)
            {
                Debug.LogWarning(
                    "MULTIPLAYER: Pion tidak mulai bergerak. " +
                    "Transaksi petak dibatalkan untuk menghindari " +
                    "pemrosesan pada posisi lama."
                );
                jumlahKembarOnline = 0;
                GiliranOnlineBerikutnya();
                yield break;
            }

           // Bila posisi berubah seketika, tetap tunggu setidaknya
// satu frame sebelum menilai status gerak.
yield return null;
yield return new WaitUntil(() => !pion.SedangBergerak);

if (pion.posisiSekarang == 9 && pion.sedangDipenjara)
{
    Debug.Log(
        "MULTIPLAYER: Player " + (pemain + 1) +
        " masuk penjara dari WP 27."
    );

    // Sinkronkan posisi dan status penjara ke semua perangkat.
    SinkronkanPosisiPenjaraOnlineRpc(pemain);

    jumlahKembarOnline = 0;

    GiliranOnlineBerikutnya();

    yield break;
}

// ==========================================
// EDUCARD: HANYA HOST MENGAMBIL KARTU
// ==========================================

if (pengelolaDekEdupoly == null)
    pengelolaDekEdupoly =
        FindAnyObjectByType<KartuEdupolyManager>();

if (pengelolaDekEdupoly != null &&
    pengelolaDekEdupoly.DatabaseSiap &&
    pengelolaDekEdupoly.ApakahPetakKartu(pion.posisiSekarang) && pion.putaranSelesai >= 1 )
{
    yield return StartCoroutine(
        ProsesEduCardOnline(pemain, pion)
    );
}

// FREE PARKING - WP 18
// Tidak ada hadiah, denda, atau kehilangan giliran.
if (pion.posisiSekarang == 18)
{
    ulong clientTujuan = pemain switch
    {
        0 => slot1.Value,
        1 => slot2.Value,
        2 => slot3.Value,
        3 => slot4.Value,
        _ => ulong.MaxValue
    };

    if (clientTujuan != ulong.MaxValue)
    {
        TampilkanInfoEduCardRpc(
            "PARKIR BEBAS\nKamu sedang beristirahat.\nTidak ada biaya yang harus dibayar.",
            RpcTarget.Single(clientTujuan, RpcTargetUse.Temp)
        );
    }

    Debug.Log(
        "FREE PARKING: Player " + (pemain + 1) +
        " berhenti di WP 18. Tidak ada perubahan saldo."
    );
}

// Sinkronkan saldo setelah pion bergerak.
            // Termasuk bonus START jika sudah diberikan
            // oleh sistem pergerakan pion di Host.
            PlayerMoneyManager uangSetelahGerak =
                FindAnyObjectByType<PlayerMoneyManager>();

            if (uangSetelahGerak != null &&
                uangSetelahGerak.saldoPemain != null &&
                pemain >= 0 &&
                pemain < uangSetelahGerak.saldoPemain.Length)
            {
                SinkronkanSaldoOnlineRpc(
                    pemain,
                    uangSetelahGerak.saldoPemain[pemain]
                );
            }

            // Proses pajak Multiplayer
            ProsesPajakOnline(pemain, pion);

            // Jangan beri reward atau proses transaksi lain
            // apabila pemain bangkrut karena pajak.
            if (ApakahPemainBangkrutOnline(pemain))
            {
                jumlahKembarOnline = 0;
                GiliranOnlineBerikutnya();
                yield break;
            }

            // Proses reward Multiplayer
            ProsesRewardOnline(pemain, pion);

            // Proses sewa lahan milik lawan
            ProsesSewaOnline(pemain, pion);
            
            // Periksa apakah lahan lawan bisa diambil alih.
            PropertyDataManager dataAmbilAlih =
                FindAnyObjectByType<PropertyDataManager>();

            if (dataAmbilAlih != null &&
                !ApakahPemainBangkrutOnline(pemain))
            {
                DataLahan target =
                    dataAmbilAlih.CariLahan(pion.posisiSekarang);

                if (target != null &&
                    target.pemilik >= 0 &&
                    target.pemilik != pemain)
                {
                    int hargaAmbilAlih =
                        HitungHargaAmbilAlihOnline(target);

                    Debug.Log(
                        "AMBIL ALIH TERSEDIA: " +
                        target.namaLahan +
                        " | Harga Rp" +
                        hargaAmbilAlih.ToString("N0")
                    );
                    
                    // Siapkan tantangan hanya jika saldo cukup.
                    PlayerMoneyManager uangAmbilAlih =
                        FindAnyObjectByType<PlayerMoneyManager>();

                    if (uangAmbilAlih != null &&
                        uangAmbilAlih.saldoPemain != null &&
                        pemain >= 0 &&
                        pemain < uangAmbilAlih.saldoPemain.Length &&
                        uangAmbilAlih.saldoPemain[pemain] >= hargaAmbilAlih)
                    {
                        
                        if (SiapkanAmbilAlihOnline(
                            pemain,
                            pion.posisiSekarang))
                        {
                            // Tampilkan panel kepada penantang.
                            TampilkanPanelAmbilAlihOnlineRpc(
                                pemain,
                                pion.posisiSekarang
                            );

                            // Tunggu jawaban atau Lewati,
                            // maksimal 60 detik.
                            float batasWaktu = 60f;

                            yield return new WaitUntil(() =>
                            {
                                batasWaktu -= Time.deltaTime;

                                return !sedangAmbilAlihOnline ||
                                       ApakahPemainTerputusOnline(pemain) ||
                                       batasWaktu <= 0f;
                            });

                            // Batalkan jika belum selesai.
                            if (sedangAmbilAlihOnline)
                            {
                                sedangAmbilAlihOnline = false;
                                pemainPenantangOnline = -1;
                                waypointAmbilAlihOnline = -1;
                                pemilikLamaOnline = -1;

                                TutupPembelianOnlineRpc();

                                Debug.Log(
                                    "Tantangan ambil alih dibatalkan."
                                );
                            }
                        }

                    }

                }
            }

            // Pemain bangkrut tidak boleh membeli/upgrade lahan
            // dan tidak boleh mengulang dadu kembar.
            if (ApakahPemainBangkrutOnline(pemain))
            {
                jumlahKembarOnline = 0;
                GiliranOnlineBerikutnya();
                yield break;
            }
        }

        // Jika pemain terputus saat animasi berlangsung,
        // lewati aksi properti dan pindahkan giliran.
        if (ApakahPemainTerputusOnline(pemain))
        {
            jumlahKembarOnline = 0;
            GiliranOnlineBerikutnya();
            yield break;
        }

        if (!masukPenjara)
        {
            PionMovement pion = turnManager.daftarPion[pemain];

            PropertyDataManager dataManager =
                FindAnyObjectByType<PropertyDataManager>();


           
            if (dataManager != null && pion.BolehBeliLahan)
            {
                DataLahan lahan =
                    dataManager.CariLahan(
                        pion.posisiSekarang
                    );

                if (lahan != null)
                {
                    // ============================
                    // LEVEL 1: PEMBELIAN LAHAN
                    // ============================
                    if (lahan.pemilik == -1 &&
                        lahan.harga > 0)
                    {
                        sedangPembelianOnline = true;
                        pemainPembeliOnline = pemain;
                        waypointPembelianOnline =
                            pion.posisiSekarang;

                        TampilkanPembelianOnlineRpc(
                            pemain,
                            pion.posisiSekarang
                        );

                        float waktuTungguPembelian = 60f;

                        yield return new WaitUntil(() =>
                        {
                            waktuTungguPembelian -=
                                Time.deltaTime;

                            return !sedangPembelianOnline ||
                                   waktuTungguPembelian <= 0f;
                        });

                        if (sedangPembelianOnline)
                        {
                            Debug.LogWarning(
                                "Waktu pembelian multiplayer habis."
                            );

                            sedangPembelianOnline = false;
                            pemainPembeliOnline = -1;
                            waypointPembelianOnline = -1;

                            TutupPembelianOnlineRpc();
                        }
                    }

                    // ============================
                    // LEVEL 2: UPGRADE LANDMARK
                    // ============================
                    else if (lahan.pemilik == pemain &&
                             lahan.tahapProperti == 1)
                    {
                        PlayerMoneyManager uang =
                            FindAnyObjectByType
                                <PlayerMoneyManager>();

                        int biayaUpgrade =
                            dataManager.HitungBiayaUpgrade(
                                lahan
                            );

                        bool saldoCukup =
                            uang != null &&
                            uang.saldoPemain != null &&
                            pemain >= 0 &&
                            pemain < uang.saldoPemain.Length &&
                            uang.saldoPemain[pemain] >=
                                biayaUpgrade;

                        if (saldoCukup)
                        {
                            sedangUpgradeOnline = true;
                            pemainUpgradeOnline = pemain;
                            waypointUpgradeOnline =
                                pion.posisiSekarang;

                            TampilkanUpgradeOnlineRpc(
                                pemain,
                                pion.posisiSekarang
                            );

                            float waktuTungguUpgrade = 60f;

                            yield return new WaitUntil(() =>
                            {
                                waktuTungguUpgrade -=
                                    Time.deltaTime;

                                return !sedangUpgradeOnline ||
                                       waktuTungguUpgrade <= 0f;
                            });

                            if (sedangUpgradeOnline)
                            {
                                Debug.LogWarning(
                                    "Waktu upgrade multiplayer habis."
                                );

                                sedangUpgradeOnline = false;
                                pemainUpgradeOnline = -1;
                                waypointUpgradeOnline = -1;

                                TutupPembelianOnlineRpc();
                            }
                        }
                        else
                        {
                            Debug.Log(
                                "Saldo tidak cukup untuk " +
                                "upgrade landmark " +
                                lahan.namaLahan
                            );
                        }
                    }
                }
            }
}
        yield return new WaitForSeconds(1f);
           // Jika kemenangan terjadi selama proses pembelian
        // atau transaksi, hentikan coroutine di sini.
        if (permainanSelesaiOnline.Value)
            yield break;

        // Jangan hidupkan timer dadu kembar untuk pemain yang
        // sudah bangkrut atau kehilangan koneksi saat menjawab soal.
        if (ApakahPemainBangkrutOnline(pemain) ||
            ApakahPemainTerputusOnline(pemain))
        {
            jumlahKembarOnline = 0;
            GiliranOnlineBerikutnya();
            yield break;
        }

        
        // Pemain bangkrut tidak boleh melempar lagi.
        // Pemain terputus juga tidak boleh mengulang giliran.
        bool pemainMasihAktif =
            !ApakahPemainBangkrutOnline(pemain) &&
            !ApakahPemainTerputusOnline(pemain);

        if (educardKehilanganGiliran)
        {
            Debug.Log("EDUCARD: pemain kehilangan 1 giliran.");
            jumlahKembarOnline = 0;
            GiliranOnlineBerikutnya();
        }
        else if ((kembar || educardMintaLemparUlang) && !masukPenjara && pemainMasihAktif)
        {
            Debug.Log("PEMAIN MENDAPAT KESEMPATAN LEMPAR DADU LAGI.");
            ResetTimerOnline();
        }
        else
        {
            jumlahKembarOnline = 0;
            GiliranOnlineBerikutnya();
        }

       
    }
   
private IEnumerator ProsesEduCardOnline(int pemain, PionMovement pion)
{
    if (!IsServer || pengelolaDekEdupoly == null)
        yield break;

    DataKartuAksiEdupoly kartu = pengelolaDekEdupoly.AmbilKartu();
    if (kartu == null)
        yield break;

    ulong clientTujuan = pemain switch
    {
        0 => slot1.Value,
        1 => slot2.Value,
        2 => slot3.Value,
        3 => slot4.Value,
        _ => ulong.MaxValue
    };

    if (clientTujuan == ulong.MaxValue)
        yield break;

    // KUIS WAWASAN UMUM
    if (kartu.kategori == "Wawasan Umum")
    {
        if (!pengelolaDekEdupoly.CobaAmbilSoalWawasanAktif(
            out DataKartuWawasanEdupoly soal))
        {
            Debug.LogWarning("EDUCARD: soal Wawasan Umum tidak tersedia.");
            yield break;
        }

        educardKuisSelesaiOnline = false;
        educardKuisBenarOnline = false;
        educardClientKuisAktifOnline = clientTujuan;

        TampilkanKuisEduCardRpc(
            soal.No,
            soal.Level,
            soal.Pertanyaan,
            soal.A,
            soal.B,
            soal.C,
            soal.D,
            soal.Jawaban,
            RpcTarget.Single(clientTujuan, RpcTargetUse.Temp)
        );

        // Tunggu jawaban pemain atau sampai batas waktu kuis terlewati.
        float batasTunggu = 40f;

        while (!educardKuisSelesaiOnline && batasTunggu > 0f)
        {
            batasTunggu -= Time.deltaTime;
            yield return null;
        }

        bool benar =
            educardKuisSelesaiOnline &&
            educardKuisBenarOnline;

        PlayerMoneyManager uangKuis =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uangKuis != null)
        {
            if (benar)
            {
                uangKuis.TambahSaldo(pemain, 200000);
            }
            else
            {
                uangKuis.KurangiSaldo(pemain, 100000);
            }

            if (uangKuis.saldoPemain != null &&
                pemain < uangKuis.saldoPemain.Length)
            {
                SinkronkanSaldoOnlineRpc(
                    pemain,
                    uangKuis.saldoPemain[pemain]
                );
            }
        }

        educardClientKuisAktifOnline = ulong.MaxValue;
        yield break;
    }

    // KARTU AKSI SELAIN WAWASAN UMUM
    string info = "EDUCARD: " + kartu.Kartu + "\n" + kartu.Aksi;

    TampilkanInfoEduCardRpc(
        info,
        RpcTarget.Single(clientTujuan, RpcTargetUse.Temp)
    );

    Debug.Log(info);

    string aksi = (kartu.Aksi ?? string.Empty).ToLowerInvariant();
    PlayerMoneyManager uang = FindAnyObjectByType<PlayerMoneyManager>();

    if (aksi.Contains("dapatkan rp") || aksi.Contains("bayar rp"))
    {
        string angka =
            System.Text.RegularExpressions.Regex.Replace(
                kartu.Aksi,
                @"[^0-9]",
                ""
            );

        int nominal;

        if (int.TryParse(angka, out nominal) &&
            nominal > 0 &&
            uang != null)
        {
            if (aksi.Contains("dapatkan rp"))
            {
                uang.TambahSaldo(pemain, nominal);
            }
            else if (!uang.KurangiSaldo(pemain, nominal))
            {
                Debug.LogWarning(
                    "EDUCARD: saldo pemain tidak cukup untuk membayar kartu."
                );
            }

            if (uang.saldoPemain != null &&
                pemain < uang.saldoPemain.Length)
            {
                SinkronkanSaldoOnlineRpc(
                    pemain,
                    uang.saldoPemain[pemain]
                );
            }
        }

        yield break;
    }

    if (aksi.Contains("lempar dadu sekali lagi"))
    {
        educardMintaLemparUlang = true;
        yield break;
    }

    if (aksi.Contains("kehilangan 1 giliran"))
    {
        educardKehilanganGiliran = true;
        yield break;
    }

    int langkah = 0;

    System.Text.RegularExpressions.Match match =
        System.Text.RegularExpressions.Regex.Match(
            aksi,
            @"(maju|mundur)\s+(\d+)"
        );

    if (match.Success)
    {
        int jumlah;

        if (int.TryParse(match.Groups[2].Value, out jumlah))
        {
            langkah =
                match.Groups[1].Value == "mundur"
                ? -jumlah
                : jumlah;
        }
    }
    else if (aksi.Contains("kembali ke petak sebelumnya"))
    {
        langkah = -1;
    }

    if (langkah != 0)
    {
        GerakkanEfekEduCardRpc(pemain, langkah);

        float batas = 10f;

        while (pion.SedangBergerak && batas > 0f)
        {
            batas -= Time.deltaTime;
            yield return null;
        }

        yield return null;
    }
    else
    {
        Debug.LogWarning(
            "EDUCARD: efek kartu ini belum otomatis didukung: " + kartu.Aksi
        );
    }
}
[Rpc(SendTo.SpecifiedInParams)]
private void TampilkanKuisEduCardRpc(
    int nomorSoal,
    string level,
    string pertanyaan,
    string pilihanA,
    string pilihanB,
    string pilihanC,
    string pilihanD,
    string kunciJawaban,
    RpcParams rpcParams = default)
{
    KartuEdupolyEffectManager effectManager =
        FindAnyObjectByType<KartuEdupolyEffectManager>();

    if (effectManager == null)
    {
        Debug.LogError(
            "EDUCARD MULTIPLAYER: KartuEdupolyEffectManager tidak ditemukan."
        );
        return;
    }

    DataKartuWawasanEdupoly soal =
        new DataKartuWawasanEdupoly
        {
            No = nomorSoal,
            Level = level,
            Pertanyaan = pertanyaan,
            A = pilihanA,
            B = pilihanB,
            C = pilihanC,
            D = pilihanD,
            Jawaban = kunciJawaban
        };

    StartCoroutine(
        effectManager.JalankanKuisMultiplayer(
            soal,
            benar =>
            {
                KirimHasilKuisEduCardServerRpc(benar);
            }
        )
    );
}
[Rpc(SendTo.Server)]
private void KirimHasilKuisEduCardServerRpc(
    bool benar,
    RpcParams rpcParams = default)
{
    if (!IsServer)
        return;

    ulong pengirim = rpcParams.Receive.SenderClientId;

    // Pastikan hasil berasal dari pemain yang sedang menjawab.
    if (pengirim != educardClientKuisAktifOnline)
    {
        Debug.LogWarning(
            "EDUCARD: Hasil kuis dari client yang tidak sesuai diabaikan."
        );
        return;
    }

    // Cegah hasil jawaban diproses lebih dari satu kali.
    if (educardKuisSelesaiOnline)
        return;

    educardKuisBenarOnline = benar;
    educardKuisSelesaiOnline = true;

    Debug.Log(
        "EDUCARD Multiplayer selesai. Hasil: " +
        (benar ? "BENAR" : "SALAH / TIMEOUT")
    );
}
[Rpc(SendTo.SpecifiedInParams)]
private void TampilkanInfoEduCardRpc(
    string pesan,
    RpcParams rpcParams = default)
{
    if (educardDisplayCoroutine != null)
    {
        StopCoroutine(educardDisplayCoroutine);
        educardDisplayCoroutine = null;
    }

    sedangTampilEduCardLokal = true;

    KartuEdupolyEffectManager effectManager =
        FindAnyObjectByType<KartuEdupolyEffectManager>();

    if (effectManager != null)
    {
        effectManager.TampilkanNotifikasiSistem(
            "EDUCARD",
            pesan,
            5f
        );
    }
    else
    {
        Debug.LogWarning(
            "EDUCARD: KartuEdupolyEffectManager tidak ditemukan."
        );

        sedangTampilEduCardLokal = false;
        return;
    }

    educardDisplayCoroutine =
        StartCoroutine(TutupTampilanEduCardLokal());
}

private IEnumerator TutupTampilanEduCardLokal()
{
    yield return new WaitForSecondsRealtime(5f);

    sedangTampilEduCardLokal = false;

    educardDisplayCoroutine = null;
}

    [Rpc(SendTo.Everyone)]
    private void GerakkanEfekEduCardRpc(int pemain, int langkah)
    {
        TurnManager tm = FindAnyObjectByType<TurnManager>();
        if (tm == null || tm.daftarPion == null || pemain < 0 || pemain >= tm.daftarPion.Length) return;
        PionMovement pion = tm.daftarPion[pemain];
        if (pion != null && !pion.SedangBergerak) pion.Bergerak(langkah);
    }

    [Rpc(SendTo.Everyone)]
    private void TutupPembelianOnlineRpc()
    {
        SoalManager soalManager =
            FindAnyObjectByType<SoalManager>();

        // Hapus callback jawaban soal lama.
        if (soalManager != null)
        {
            if (callbackSoalOnline != null)
            {
                soalManager.OnJawabanSelesai -=
                    callbackSoalOnline;
            }

            soalManager.BatalkanSoal();
        }

        callbackSoalOnline = null;

        // Sembunyikan kartu soal.
        CardDisplayManager kartu =
            FindAnyObjectByType<CardDisplayManager>();

        if (kartu != null)
        {
            kartu.SembunyikanKartu();
        }

        // Tutup panel pembelian / upgrade.
        PropertyPurchaseUI purchaseUI =
            FindAnyObjectByType<PropertyPurchaseUI>();

        if (purchaseUI != null)
        {
            purchaseUI.TutupPanel();
            purchaseUI.saatKlikBeli = null;
            purchaseUI.saatKlikLewati = null;
        }

        Debug.Log(
            "MULTIPLAYER: Panel dan callback soal dibersihkan."
        );
    }

    [Rpc(SendTo.Everyone)]
    private void SinkronkanSaldoOnlineRpc(int pemain, int saldoBaru)
    {
        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang == null || uang.saldoPemain == null ||
            pemain < 0 || pemain >= uang.saldoPemain.Length)
        {
            Debug.LogWarning("Data saldo multiplayer tidak ditemukan!");
            return;
        }

        uang.saldoPemain[pemain] = saldoBaru;
        uang.PerbaruiUIPemain();

        Debug.Log(
            "MULTIPLAYER: Saldo Player " + (pemain + 1) +
            " disinkronkan menjadi Rp " + saldoBaru.ToString("N0")
        );
    }
    [Rpc(SendTo.Everyone)]
    private void SinkronkanPemilikLahanRpc(
    int waypoint,
    int pemilik)
    {
        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        if (dataManager == null)
        {
            Debug.LogError("PropertyDataManager tidak ditemukan!");
            return;
        }

        DataLahan lahan = dataManager.CariLahan(waypoint);

        if (lahan == null)
        {
            Debug.LogWarning("Lahan tidak ditemukan!");
            return;
        }

        dataManager.TerapkanPemilikDariJaringan(
        waypoint,
        pemilik
        );

        Debug.Log(
            "MULTIPLAYER: Lahan " + lahan.namaLahan +
            " dimiliki Player " + (pemilik + 1)
        );
    }
    
    // ==========================================
    // PANEL UPGRADE LANDMARK MULTIPLAYER
    // ==========================================
    [Rpc(SendTo.Everyone)]
    private void TampilkanUpgradeOnlineRpc(
        int pemain,
        int waypoint)
    {
        // Panel hanya muncul pada perangkat
        // pemain yang sedang mendapat giliran.
        if (playerIndexLokal != pemain)
            return;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PropertyPurchaseUI purchaseUI =
            FindAnyObjectByType<PropertyPurchaseUI>();

        SoalManager soalManager =
            FindAnyObjectByType<SoalManager>();

        CardDisplayManager kartu =
            FindAnyObjectByType<CardDisplayManager>();

        if (dataManager == null ||
            purchaseUI == null ||
            soalManager == null ||
            kartu == null)
        {
            Debug.LogError(
                "Referensi upgrade Multiplayer belum lengkap!"
            );
            return;
        }

        DataLahan lahan =
            dataManager.CariLahan(waypoint);

        if (lahan == null ||
            lahan.pemilik != pemain ||
            lahan.tahapProperti != 1)
        {
            Debug.LogWarning(
                "Lahan tidak memenuhi syarat upgrade."
            );
            return;
        }

        int biayaUpgrade =
            dataManager.HitungBiayaUpgrade(lahan);

        bool sudahMemilih = false;
        bool sedangMenjawab = false;

        System.Action<bool> hasilJawaban = null;

        // Bersihkan callback tombol sebelumnya.
        purchaseUI.saatKlikBeli = null;
        purchaseUI.saatKlikLewati = null;

        // =====================================
        // TOMBOL BELI = UPGRADE
        // =====================================
        purchaseUI.saatKlikBeli = () =>
        {
            if (sudahMemilih || sedangMenjawab)
                return;

            if (!sedangProsesOnline.Value)
                return;

            sedangMenjawab = true;

            hasilJawaban = benar =>
            {
                soalManager.OnJawabanSelesai -=
                    hasilJawaban;

                if (!sedangProsesOnline.Value)
                    return;

                sudahMemilih = true;
                sedangMenjawab = false;

                KirimHasilUpgradeOnlineRpc(
                    pemain,
                    waypoint,
                    benar
                );
            };

            // Simpan callback agar penutupan panel oleh Host
            // juga dapat melepaskan handler Level 2.
            callbackSoalOnline = hasilJawaban;
            soalManager.OnJawabanSelesai += hasilJawaban;

            // Upgrade selalu memakai Level 2.
            bool berhasilTampil =
                kartu.TampilkanKartuSesuaiPetak(
                    lahan.namaLahan,
                    2
                );

            if (!berhasilTampil)
            {
                soalManager.OnJawabanSelesai -=
                    hasilJawaban;

                sedangMenjawab = false;

                Debug.LogWarning(
                    "Kartu Level 2 gagal muncul!"
                );
            }
        };

        // =====================================
        // TOMBOL LEWATI
        // =====================================
        purchaseUI.saatKlikLewati = () =>
        {
            if (sudahMemilih || sedangMenjawab)
                return;

            sudahMemilih = true;

            KirimLewatiUpgradeOnlineRpc(
                pemain,
                waypoint
            );
        };

        // Gunakan panel pembelian sementara.
        purchaseUI.TampilkanPembelian(
            "UPGRADE " + lahan.namaLahan,
            biayaUpgrade,
            lahan.warnaKomplek
        );

        Debug.Log(
            "MULTIPLAYER: Upgrade tersedia di " +
            lahan.namaLahan +
            " | Biaya Rp" +
            biayaUpgrade.ToString("N0")
        );
    }

    // ==========================================
    // PEMAIN MELEWATI UPGRADE
    // ==========================================
    [Rpc(SendTo.Server)]
    private void KirimLewatiUpgradeOnlineRpc(
        int pemain,
        int waypoint,
        RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong pengirim =
            rpcParams.Receive.SenderClientId;

        ulong pemilikSlot = pemain switch
        {
            0 => slot1.Value,
            1 => slot2.Value,
            2 => slot3.Value,
            3 => slot4.Value,
            _ => ulong.MaxValue
        };

        if (pengirim != pemilikSlot ||
            pemain != giliranOnline.Value ||
            !sedangUpgradeOnline ||
            pemain != pemainUpgradeOnline ||
            waypoint != waypointUpgradeOnline)
        {
            Debug.LogWarning(
                "Permintaan lewati upgrade ditolak."
            );
            return;
        }

        // Pastikan pemain masih di petak tersebut.
        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        if (turnManager == null ||
            turnManager.daftarPion == null ||
            pemain < 0 ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null ||
            turnManager.daftarPion[pemain]
                .posisiSekarang != waypoint)
        {
            return;
        }

        sedangUpgradeOnline = false;
        pemainUpgradeOnline = -1;
        waypointUpgradeOnline = -1;

        TutupPembelianOnlineRpc();

        Debug.Log(
            "HOST: Player " + (pemain + 1) +
            " melewati upgrade landmark."
        );
    }

    [Rpc(SendTo.Everyone)]
    private void TampilkanPembelianOnlineRpc(
        int pemain,
        int waypoint)
    {
        // Panel hanya untuk pemain yang sedang giliran
        if (playerIndexLokal != pemain)
            return;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PropertyPurchaseUI purchaseUI =
            FindAnyObjectByType<PropertyPurchaseUI>();

        SoalManager soalManager =
            FindAnyObjectByType<SoalManager>();

        CardDisplayManager kartu =
            FindAnyObjectByType<CardDisplayManager>();

        if (dataManager == null ||
            purchaseUI == null ||
            soalManager == null ||
            kartu == null)
        {
            Debug.LogError(
                "Referensi pembelian Multiplayer belum lengkap!"
            );
            return;
        }

        DataLahan lahan =
            dataManager.CariLahan(waypoint);

        if (lahan == null)
            return;

        bool sudahMemilih = false;
        bool sedangMenjawab = false;

        System.Action<bool> hasilJawaban = null;

        // Bersihkan callback sebelumnya
        purchaseUI.saatKlikBeli = null;
        purchaseUI.saatKlikLewati = null;

        // ====================================
        // TOMBOL BELI
        // ====================================
        purchaseUI.saatKlikBeli = () =>
        {
            if (sudahMemilih || sedangMenjawab)
                return;

            if (!sedangProsesOnline.Value)
                return;

            sedangMenjawab = true;

            Debug.Log(
                "MULTIPLAYER: Player " + (pemain + 1) +
                " memilih BELI " + lahan.namaLahan
            );

            hasilJawaban = (benar) =>
            {
                soalManager.OnJawabanSelesai -= hasilJawaban;

                if (!sedangProsesOnline.Value)
                    return;

                sudahMemilih = true;
                sedangMenjawab = false;

                KirimHasilSoalOnlineRpc(
                    pemain,
                    waypoint,
                    benar
                );
            };

           callbackSoalOnline = hasilJawaban;
soalManager.OnJawabanSelesai += hasilJawaban;

            // Kartu soal baru muncul setelah tombol Beli
            bool berhasilTampil =
                kartu.TampilkanKartuSesuaiPetak(
                    lahan.namaLahan
                );

            if (!berhasilTampil)
            {
                soalManager.OnJawabanSelesai -= hasilJawaban;
                sedangMenjawab = false;

                Debug.LogWarning(
                    "Kartu soal pembelian gagal ditampilkan!"
                );
            }
        };

        // ====================================
        // TOMBOL LEWATI
        // ====================================
        purchaseUI.saatKlikLewati = () =>
        {
            if (sudahMemilih || sedangMenjawab)
                return;

            sudahMemilih = true;

            Debug.Log(
                "MULTIPLAYER: Player " + (pemain + 1) +
                " memilih LEWATI " + lahan.namaLahan
            );

            // Hanya dikirim ketika tombol Lewati diklik
            KirimLewatiPembelianOnlineRpc(
                pemain,
                waypoint
            );
        };

        // ====================================
        // TAMPILKAN PANEL PEMBELIAN
        // ====================================
        purchaseUI.TampilkanPembelian(
            lahan.namaLahan,
            lahan.harga,
            lahan.warnaKomplek
        );

        Debug.Log(
            "MULTIPLAYER: Panel pembelian muncul untuk " +
            lahan.namaLahan
        );
    }

    [Rpc(SendTo.Server)]
    private void KirimLewatiPembelianOnlineRpc(
    int pemain,
    int waypoint,
    RpcParams rpcParams = default)
    {
        ulong pengirim = rpcParams.Receive.SenderClientId;

        ulong pemilikSlot = pemain switch
        {
            0 => slot1.Value,
            1 => slot2.Value,
            2 => slot3.Value,
            3 => slot4.Value,
            _ => ulong.MaxValue
        };

        if (pengirim != pemilikSlot ||
            pemain != giliranOnline.Value ||
            !sedangPembelianOnline ||
            pemain != pemainPembeliOnline)
            return;

        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        if (turnManager == null ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null ||
            turnManager.daftarPion[pemain].posisiSekarang != waypoint)
            return;

        sedangPembelianOnline = false;
        pemainPembeliOnline = -1;
        waypointPembelianOnline = -1;
        TutupPembelianOnlineRpc();

        Debug.Log("HOST: Pemain melewati pembelian lahan.");
    }

    [Rpc(SendTo.Server)]
    private void KirimHasilSoalOnlineRpc(
    int pemain,
    int waypoint,
    bool benar,

    RpcParams rpcParams = default)
    {
        if (!IsServer ||
    permainanSelesaiOnline.Value ||
    ApakahPemainBangkrutOnline(pemain) ||
    ApakahPemainTerputusOnline(pemain))
{
    return;
}
        ulong pengirim = rpcParams.Receive.SenderClientId;

        ulong pemilikSlot = pemain switch
        {
            0 => slot1.Value,
            1 => slot2.Value,
            2 => slot3.Value,
            3 => slot4.Value,
            _ => ulong.MaxValue
        };

        if (pengirim != pemilikSlot ||
            pemain != giliranOnline.Value ||
            !sedangPembelianOnline ||
            pemain != pemainPembeliOnline ||
            waypoint != waypointPembelianOnline)
        {
            Debug.LogWarning("Hasil soal multiplayer ditolak.");
            return;
        }

        Debug.Log(
            "HOST menerima hasil soal pemain " +
            (pemain + 1) +
            " pada waypoint " + waypoint +
            ": " + (benar ? "BENAR" : "SALAH")
        );
        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (turnManager == null ||
            dataManager == null ||
            turnManager.daftarPion == null ||
            pemain < 0 ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null)
        {
            Debug.LogWarning("Data pembelian multiplayer tidak lengkap.");
            sedangPembelianOnline = false;
            pemainPembeliOnline = -1;
            return;
        }

        if (turnManager.daftarPion[pemain].posisiSekarang != waypoint)
        {
            Debug.LogWarning("Waypoint pembelian tidak sesuai.");
            return;
        }

        DataLahan lahan = dataManager.CariLahan(waypoint);

        if (lahan != null && lahan.pemilik == -1 && lahan.harga > 0)
        {
            if (benar && uang != null)
            {
                dataManager.BeliLahan(lahan, pemain, uang);

                if (lahan.pemilik == pemain)
                {
                    SinkronkanPemilikLahanRpc(waypoint, pemain);
CekKemenanganSatuSisiOnline();
                    if (uang.saldoPemain != null &&
                        pemain >= 0 &&
                        pemain < uang.saldoPemain.Length)
                    {
                        SinkronkanSaldoOnlineRpc(
                            pemain,
                            uang.saldoPemain[pemain]
                        );
                    }
                }
            }
        }
        sedangPembelianOnline = false;
        pemainPembeliOnline = -1;
        waypointPembelianOnline = -1;
        TutupPembelianOnlineRpc();
    }
    
    // =========================================
    // HASIL SOAL LEVEL 2 MULTIPLAYER
    // =========================================
    [Rpc(SendTo.Server)]
    private void KirimHasilUpgradeOnlineRpc(
        int pemain,
        int waypoint,
        bool benar,
        RpcParams rpcParams = default)
    {
        if (!IsServer ||
    permainanSelesaiOnline.Value ||
    ApakahPemainBangkrutOnline(pemain) ||
    ApakahPemainTerputusOnline(pemain))
{
    return;
}

        ulong pengirim =
            rpcParams.Receive.SenderClientId;

        ulong pemilikSlot = pemain switch
        {
            0 => slot1.Value,
            1 => slot2.Value,
            2 => slot3.Value,
            3 => slot4.Value,
            _ => ulong.MaxValue
        };

        if (pengirim != pemilikSlot ||
            pemain != giliranOnline.Value ||
            !sedangUpgradeOnline ||
            pemain != pemainUpgradeOnline ||
            waypoint != waypointUpgradeOnline)
        {
            Debug.LogWarning(
                "HOST: Hasil upgrade ditolak."
            );
            return;
        }

        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (turnManager == null ||
            dataManager == null ||
            uang == null ||
            turnManager.daftarPion == null ||
            pemain < 0 ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null)
        {
            Debug.LogWarning(
                "HOST: Data upgrade belum lengkap."
            );

            sedangUpgradeOnline = false;
            pemainUpgradeOnline = -1;
            waypointUpgradeOnline = -1;

            TutupPembelianOnlineRpc();
            return;
        }

        PionMovement pion =
            turnManager.daftarPion[pemain];

        if (pion.posisiSekarang != waypoint)
        {
            Debug.LogWarning(
                "HOST: Posisi upgrade tidak sesuai."
            );
            return;
        }

        DataLahan lahan =
            dataManager.CariLahan(waypoint);

        sedangUpgradeOnline = false;
        pemainUpgradeOnline = -1;
        waypointUpgradeOnline = -1;

        if (lahan == null ||
            lahan.pemilik != pemain ||
            lahan.tahapProperti != 1)
        {
            Debug.LogWarning(
                "HOST: Lahan tidak memenuhi syarat upgrade."
            );

            TutupPembelianOnlineRpc();
            return;
        }

        if (!benar)
        {
            Debug.Log(
                "HOST: Jawaban Level 2 salah."
            );

            TutupPembelianOnlineRpc();
            return;
        }

        // Pembayaran hanya diproses oleh Host.
        bool berhasil =
            dataManager.UpgradeLandmark(
                lahan,
                pemain,
                uang
            );

        if (berhasil)
        {
            SinkronkanLandmarkOnlineRpc(
                waypoint,
                2
            );

            if (uang.saldoPemain != null &&
                pemain < uang.saldoPemain.Length)
            {
                SinkronkanSaldoOnlineRpc(
                    pemain,
                    uang.saldoPemain[pemain]
                );
            }

            Debug.Log(
                "HOST: Landmark berhasil di-upgrade: " +
                lahan.namaLahan
            );
        }
        else
        {
            Debug.LogWarning(
                "HOST: Upgrade gagal, saldo tidak cukup."
            );
        }

        TutupPembelianOnlineRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void TampilkanLemparanRpc(
        int pemain,
        int angka1,
        int angka2,
        bool masukPenjara
    )
    {
        StartCoroutine(
            AnimasiLemparanOnline(
                pemain,
                angka1,
                angka2,
                masukPenjara
            )
        );
    }

    private IEnumerator AnimasiLemparanOnline(
        int pemain,
        int angka1,
        int angka2,
        bool masukPenjara
    )
    {
        DiceTotal dadu =
            FindAnyObjectByType<DiceTotal>();

        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        if (dadu == null ||
            turnManager == null ||
            dadu.diceMix == null ||
            dadu.dice1 == null ||
            dadu.dice2 == null ||
            turnManager.daftarPion == null ||
            pemain < 0 ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null)
        {
            Debug.LogError("Data lemparan online tidak lengkap!");
            yield break;
        }

        // Perbarui giliran yang terlihat secara lokal
        turnManager.giliranSekarang = pemain;

        // Kocok dua dadu di setiap perangkat
        yield return StartCoroutine(
            dadu.diceMix.KocokDadu()
        );

        // Paksa hasil yang sama dari Host
        dadu.dice1.SetResult(angka1);
        dadu.dice2.SetResult(angka2);

        PionMovement pion =
            turnManager.daftarPion[pemain];

        if (masukPenjara)
        {
            pion.MasukPenjara();
        }
        else
        {
            pion.Bergerak(angka1 + angka2);
        }

        Debug.Log(
            "NETWORK PLAYER " + (pemain + 1) +
            ": " + angka1 + " + " + angka2 +
            " = " + (angka1 + angka2)
        );

    } 
    
    // Menghitung harga pengambilalihan properti lawan.
    private int HitungHargaAmbilAlihOnline(DataLahan lahan)
    {
        if (lahan == null)
            return 0;

        // Tahap 1: 150% harga lahan.
        // Tahap 2: 200% harga lahan.
        float pengali = lahan.tahapProperti == 2
            ? 2f
            : 1.5f;

        return Mathf.RoundToInt(lahan.harga * pengali);
    }

    // Menyiapkan tantangan ambil alih milik lawan.
    private bool SiapkanAmbilAlihOnline(
        int pemain,
        int waypoint)
    {
        if (!IsServer ||
            permainanSelesaiOnline.Value ||
            sedangAmbilAlihOnline ||
            ApakahPemainBangkrutOnline(pemain))
            return false;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        if (dataManager == null)
            return false;

        DataLahan lahan = dataManager.CariLahan(waypoint);

        if (lahan == null ||
            lahan.pemilik < 0 ||
            lahan.pemilik == pemain)
            return false;

        sedangAmbilAlihOnline = true;
        pemainPenantangOnline = pemain;
        waypointAmbilAlihOnline = waypoint;
        pemilikLamaOnline = lahan.pemilik;

        Debug.Log(
            "TANTANGAN AMBIL ALIH: Player " +
            (pemain + 1) + " menantang " +
            lahan.namaLahan
        );

        return true;
    }

    // Host memvalidasi dan memproses ambil alih lahan.
    [Rpc(SendTo.Server)]
    private void KirimHasilAmbilAlihOnlineRpc(
        int pemain,
        int waypoint,
        bool benar,
        RpcParams rpcParams = default)
    {
        if (!IsServer ||
            permainanSelesaiOnline.Value ||
            !sedangAmbilAlihOnline)
            return;

        ulong pengirim = rpcParams.Receive.SenderClientId;

        ulong pemilikSlot = pemain switch
        {
            0 => slot1.Value,
            1 => slot2.Value,
            2 => slot3.Value,
            3 => slot4.Value,
            _ => ulong.MaxValue
        };

        
        // Pastikan permintaan berasal dari pemain
        // yang sah dan masih terhubung.
        if (pengirim != pemilikSlot ||
            pemain != giliranOnline.Value ||
            pemain != pemainPenantangOnline ||
            waypoint != waypointAmbilAlihOnline ||
            ApakahPemainTerputusOnline(pemain) ||
            ApakahPemainBangkrutOnline(pemain))
        {
            Debug.LogWarning(
                "Hasil ambil alih ditolak: pemain tidak valid."
            );
            return;
        }


        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        
        if (dataManager == null ||
            uang == null ||
            uang.saldoPemain == null ||
            turnManager == null ||
            turnManager.daftarPion == null ||
            pemain < 0 ||
            pemain >= uang.saldoPemain.Length ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null)
        {
            // Bersihkan status tantangan.
            sedangAmbilAlihOnline = false;
            pemainPenantangOnline = -1;
            waypointAmbilAlihOnline = -1;
            pemilikLamaOnline = -1;

            TutupPembelianOnlineRpc();

            Debug.LogWarning(
                "Ambil alih dibatalkan: data permainan tidak lengkap."
            );

            return;
        }


        DataLahan lahan = dataManager.CariLahan(waypoint);
        int pemilikLama = pemilikLamaOnline;

        // Pastikan penantang masih berada di lahan lawan.
        
        if (lahan == null ||
            lahan.pemilik != pemilikLama ||
            pemilikLama < 0 ||
            pemilikLama >= uang.saldoPemain.Length ||
            ApakahPemainBangkrutOnline(pemilikLama) ||
            turnManager.daftarPion[pemain].posisiSekarang != waypoint)
        
        {
            // Batalkan transaksi yang tidak valid.
            sedangAmbilAlihOnline = false;
            pemainPenantangOnline = -1;
            waypointAmbilAlihOnline = -1;
            pemilikLamaOnline = -1;

            // Tutup panel dan bersihkan callback soal.
            TutupPembelianOnlineRpc();

            Debug.LogWarning(
                "Ambil alih dibatalkan: pemilik atau posisi tidak valid."
            );

            return;
        }



        int harga = HitungHargaAmbilAlihOnline(lahan);

        // Tantangan selesai setelah hasil diterima.
        sedangAmbilAlihOnline = false;
        pemainPenantangOnline = -1;
        waypointAmbilAlihOnline = -1;
        pemilikLamaOnline = -1;

        if (benar && harga > 0 &&
            !ApakahPemainBangkrutOnline(pemain) &&
            uang.KurangiSaldo(pemain, harga))
        {
            // Uang dibayarkan kepada pemilik sebelumnya.
            uang.TambahSaldo(pemilikLama, harga);

            // Pertahankan tahap rumah/landmark.
            lahan.pemilik = pemain;

            SinkronkanPemilikLahanRpc(waypoint, pemain);

            SinkronkanSaldoOnlineRpc(
                pemain,
                uang.saldoPemain[pemain]
            );

            SinkronkanSaldoOnlineRpc(
                pemilikLama,
                uang.saldoPemain[pemilikLama]
            );

            CekKemenanganSatuSisiOnline();

            Debug.Log(
                "AMBIL ALIH BERHASIL: Player " +
                (pemain + 1) + " mendapatkan " +
                lahan.namaLahan
            );
        }
        else
        {
            Debug.Log(
                "AMBIL ALIH GAGAL: Jawaban salah " +
                "atau saldo tidak mencukupi."
            );
        }

        TutupPembelianOnlineRpc();
    }

    [Rpc(SendTo.Server)]
    private void KirimLewatiAmbilAlihOnlineRpc(
        int pemain,
        int waypoint,
        RpcParams rpcParams = default)
    {
        if (!IsServer || !sedangAmbilAlihOnline)
            return;

        ulong pengirim = rpcParams.Receive.SenderClientId;

        ulong pemilikSlot = pemain switch
        {
            0 => slot1.Value,
            1 => slot2.Value,
            2 => slot3.Value,
            3 => slot4.Value,
            _ => ulong.MaxValue
        };

        if (pengirim != pemilikSlot ||
            pemain != pemainPenantangOnline ||
            pemain != giliranOnline.Value ||
            waypoint != waypointAmbilAlihOnline)
            return;

        sedangAmbilAlihOnline = false;
        pemainPenantangOnline = -1;
        waypointAmbilAlihOnline = -1;
        pemilikLamaOnline = -1;

        TutupPembelianOnlineRpc();

        Debug.Log("MULTIPLAYER: Tantangan ambil alih dilewati.");
    }

    // Panel tantangan ambil alih untuk pemain aktif.
    [Rpc(SendTo.Everyone)]
    private void TampilkanPanelAmbilAlihOnlineRpc(
        int pemain,
        int waypoint)
    {
        if (playerIndexLokal != pemain)
            return;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PropertyPurchaseUI purchaseUI =
            FindAnyObjectByType<PropertyPurchaseUI>();

        if (dataManager == null || purchaseUI == null)
            return;

        DataLahan lahan = dataManager.CariLahan(waypoint);

        if (lahan == null)
            return;

        int hargaAmbilAlih = HitungHargaAmbilAlihOnline(lahan);

        
       
        // Tombol BELI digunakan untuk menantang pemilik lahan.
        purchaseUI.saatKlikBeli = () =>
        {
            SoalManager soalManager =
                FindAnyObjectByType<SoalManager>();

            CardDisplayManager kartu =
                FindAnyObjectByType<CardDisplayManager>();

            if (soalManager == null || kartu == null)
            {
                Debug.LogError(
                    "SoalManager atau CardDisplayManager tidak ditemukan!"
                );
                return;
            }

            // Hindari callback ganda.
            if (callbackSoalOnline != null)
                return;

            System.Action<bool> hasilJawaban = null;

            hasilJawaban = benar =>
            {
                soalManager.OnJawabanSelesai -= hasilJawaban;
                callbackSoalOnline = null;

                
                Debug.Log(
                    "HASIL TANTANGAN LEVEL 2: " +
                    (benar ? "BENAR" : "SALAH")
                );

                // Kirim hasil soal ke Host.
                // Host memvalidasi pemain, posisi,
                // kepemilikan lahan, dan saldo.
                KirimHasilAmbilAlihOnlineRpc(
                    pemain,
                    waypoint,
                    benar
                );

            };

            callbackSoalOnline = hasilJawaban;
            soalManager.OnJawabanSelesai += hasilJawaban;

            bool berhasilTampil =
                kartu.TampilkanKartuSesuaiPetak(
                    lahan.namaLahan,
                    2
                );

            if (!berhasilTampil)
            {
                soalManager.OnJawabanSelesai -= hasilJawaban;
                callbackSoalOnline = null;

                Debug.LogWarning(
                    "Kartu soal Level 2 gagal ditampilkan!"
                );
            }
        };


        // Tombol LEWATI membatalkan tantangan.
        purchaseUI.saatKlikLewati = () =>
        {
            KirimLewatiAmbilAlihOnlineRpc(pemain, waypoint);
        };


        purchaseUI.TampilkanPembelian(
            "AMBIL ALIH " + lahan.namaLahan,
            hargaAmbilAlih,
            lahan.warnaKomplek
        );

        Debug.Log(
            "Panel tantangan ambil alih ditampilkan."
        );
    }

    // ==========================================
    // SISTEM SEWA LAHAN MULTIPLAYER
    // ==========================================
    private void ProsesSewaOnline(
        int pemain,
        PionMovement pion)
    {
        // Semua transaksi hanya diproses Host.
        if (!IsServer ||
    pion == null ||
    permainanSelesaiOnline.Value ||
    ApakahPemainBangkrutOnline(pemain) ||
    ApakahPemainTerputusOnline(pemain))
{
    return;
}
        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (dataManager == null ||
            uang == null ||
            uang.saldoPemain == null)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Data sewa belum lengkap!"
            );
            return;
        }

        DataLahan lahan =
            dataManager.CariLahan(
                pion.posisiSekarang
            );

        // Petak ini bukan lahan yang bisa disewa.
        if (lahan == null)
            return;

        int pemilik = lahan.pemilik;

        // Tidak bayar jika lahan kosong
        // atau milik pemain sendiri.
        // Lahan kosong atau milik sendiri tidak dikenai sewa.
// Pemilik bangkrut juga tidak boleh menerima uang sewa.
if (pemilik < 0 ||
    pemilik == pemain ||
    ApakahPemainBangkrutOnline(pemilik))
{
    return;
}

        if (pemain < 0 ||
            pemain >= uang.saldoPemain.Length ||
            pemilik >= uang.saldoPemain.Length)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Indeks pemain sewa tidak valid."
            );
            return;
        }

        int biayaSewa = 0;

        // Tahap 1: tiga bangunan.
        if (lahan.tahapProperti == 1)
        {
            biayaSewa = Mathf.RoundToInt(
                lahan.harga * 0.10f
            );
        }
        // Tahap 2: landmark.
        else if (lahan.tahapProperti == 2)
        {
            biayaSewa = Mathf.RoundToInt(
                lahan.harga * 0.25f
            );
        }

        if (biayaSewa <= 0)
            return;

       
        // Jika saldo tidak cukup, pemain bangkrut.
        if (uang.saldoPemain[pemain] < biayaSewa)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Player " +
                (pemain + 1) +
                " tidak mampu membayar sewa Rp" +
                biayaSewa.ToString("N0") +
                " di " + lahan.namaLahan
            );

            // Host menandai pemain sebagai bangkrut.
            TandaiBangkrutOnline(pemain);

            // Jangan lanjutkan transaksi sewa.
            return;
        }


        // Host memotong saldo penyewa.
        bool berhasil =
            uang.KurangiSaldo(
                pemain,
                biayaSewa
            );

        if (!berhasil)
            return;

        // Host memberikan uang kepada pemilik.
        uang.TambahSaldo(
            pemilik,
            biayaSewa
        );

        // Sinkronkan kedua saldo ke semua perangkat.
        SinkronkanSaldoOnlineRpc(
            pemain,
            uang.saldoPemain[pemain]
        );

        SinkronkanSaldoOnlineRpc(
            pemilik,
            uang.saldoPemain[pemilik]
        );

        Debug.Log(
            "MULTIPLAYER: Player " +
            (pemain + 1) +
            " membayar sewa Rp" +
            biayaSewa.ToString("N0") +
            " kepada Player " +
            (pemilik + 1) +
            " di " + lahan.namaLahan
        );
    }

// ======================================
// SISTEM PAJAK MULTIPLAYER EDUPOLY
// ======================================

private void ProsesPajakOnline(int pemain, PionMovement pion)
{
    if (pion == null || pion.putaranSelesai < 1)
        return;
    if (!IsServer || pion == null)
        return;

    int biayaPajak = 0;

    // WP 5
    if (pion.posisiSekarang == 5)
    {
        biayaPajak = 100000;
    }
    // WP 33
    else if (pion.posisiSekarang == 33)
    {
        biayaPajak = 150000;
    }

    if (biayaPajak == 0)
        return;

    PlayerMoneyManager uang =
        FindAnyObjectByType<PlayerMoneyManager>();

    if (uang == null)
    {
        Debug.LogError("PlayerMoneyManager tidak ditemukan!");
        return;
    }

    if (uang.saldoPemain == null ||
        pemain < 0 ||
        pemain >= uang.saldoPemain.Length)
        return;

    bool berhasil =
        uang.KurangiSaldo(pemain, biayaPajak);

    if (!berhasil)
    {
        Debug.LogWarning(
            "Saldo Player " + (pemain + 1) +
            " tidak cukup untuk membayar pajak!"
        );
        TandaiBangkrutOnline(pemain);
        return;
    }

    SinkronkanSaldoOnlineRpc(
        pemain,
        uang.saldoPemain[pemain]
    );

    Debug.Log(
        "HOST: Player " + (pemain + 1) +
        " membayar pajak Rp" +
        biayaPajak.ToString("N0")
    );
}

    // ======================================
    // SISTEM REWARD MULTIPLAYER EDU-POLY
    // WP 11 DAN WP 20 = Rp50.000
    // ======================================

    private void ProsesRewardOnline(int pemain, PionMovement pion)
    {
        if (pion == null || pion.putaranSelesai < 1)
            return;
        // Hanya Host yang boleh menambahkan saldo
        if (!IsServer || pion == null)
            return;

        int jumlahReward = 0;

        if (pion.posisiSekarang == 11 ||
            pion.posisiSekarang == 20)
        {
            jumlahReward = 50000;
        }

        if (jumlahReward == 0)
            return;

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang == null || uang.saldoPemain == null)
        {
            Debug.LogWarning(
                "Data uang pemain tidak ditemukan!"
            );
            return;
        }

        if (pemain < 0 || pemain >= uang.saldoPemain.Length)
            return;

        // Tambah uang hanya sekali melalui Host
        uang.TambahSaldo(pemain, jumlahReward);

        // Kirim saldo terbaru ke seluruh pemain
        SinkronkanSaldoOnlineRpc(
            pemain,
            uang.saldoPemain[pemain]
        );

        Debug.Log(
            "REWARD MULTIPLAYER: Player " +
            (pemain + 1) +
            " mendapat Rp" +
            jumlahReward.ToString("N0") +
            " pada WP " + pion.posisiSekarang
        );
    }

    // =========================================
// DENDA PENJARA MULTIPLAYER Rp100.000
// =========================================

private bool BayarDendaPenjaraOnline(int pemain)
{
    if (!IsServer)
        return false;

    TurnManager turnManager =
        FindAnyObjectByType<TurnManager>();

    PlayerMoneyManager uang =
        FindAnyObjectByType<PlayerMoneyManager>();

    if (turnManager == null ||
        uang == null ||
        turnManager.daftarPion == null ||
        uang.saldoPemain == null)
    {
        Debug.LogWarning(
            "Data denda penjara Multiplayer belum lengkap!"
        );
        return false;
    }

    if (pemain < 0 ||
        pemain >= turnManager.daftarPion.Length ||
        pemain >= uang.saldoPemain.Length)
        return false;

    PionMovement pion =
        turnManager.daftarPion[pemain];

    if (pion == null)
        return false;

    // Jika tidak dipenjara, boleh bermain biasa.
    if (!pion.sedangDipenjara)
        return true;

    const int denda = 100000;

    // Jika saldo tidak cukup, lewati giliran.
    if (!uang.KurangiSaldo(pemain, denda))
    {
        Debug.LogWarning(
            "MULTIPLAYER: Player " + (pemain + 1) +
            " belum mampu membayar denda penjara. Giliran dilewati."
        );

        return false;
    }

    // Bebaskan pemain.
    pion.sedangDipenjara = false;

    SinkronkanSaldoOnlineRpc(
        pemain,
        uang.saldoPemain[pemain]
    );

    SinkronkanStatusPenjaraOnlineRpc(
        pemain,
        false
    );

    Debug.Log(
        "MULTIPLAYER: Player " + (pemain + 1) +
        " membayar denda Rp100.000 dan bebas!"
    );

    return true;
}


    // Menang jika hanya tersisa satu pemain aktif.
    private void CekKemenanganPemainAktifOnline()
    {
        if (!IsServer || permainanSelesaiOnline.Value)
            return;

        int pemainAktif = -1;
        int jumlahAktif = 0;
        int jumlah = Mathf.Clamp(jumlahPemainOnline, 1, 4);

        for (int i = 0; i < jumlah; i++)
        {
            if (ApakahPemainBangkrutOnline(i) ||
                ApakahPemainTerputusOnline(i))
                continue;

            pemainAktif = i;
            jumlahAktif++;
        }

        if (jumlahAktif > 1)
            return;

        permainanSelesaiOnline.Value = true;
        sedangProsesOnline.Value = true;
        waktuOnline.Value = 0;
        sisaWaktuHost = 0f;

        if (pemainAktif >= 0)
        {
            Debug.Log(
                "EDU-POLY MULTIPLAYER SELESAI! PLAYER " +
                (pemainAktif + 1) + " MENANG. Pemain lain bangkrut/menyerah."
            );
            UmumkanPemenangOnlineRpc(pemainAktif);
        }
        else
        {
            Debug.LogWarning(
                "EDU-POLY MULTIPLAYER SELESAI TANPA PEMENANG: " +
                "semua pemain bangkrut atau menyerah."
            );
        }
    }

    // ==========================================
    // CEK KEMENANGAN SATU SISI MULTIPLAYER
    // ==========================================
    private void CekKemenanganSatuSisiOnline()
    {
        // Hanya Host yang menentukan pemenang.
        if (!IsServer)
            return;

        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        if (dataManager == null)
        {
            Debug.LogWarning(
                "MULTIPLAYER: PropertyDataManager tidak ditemukan."
            );
            return;
        }

        int pemenang =
            SideOwnershipWinChecker.CariPemenangSatuSisi(
                dataManager
            );

        // Belum ada pemain yang menguasai satu sisi.
        if (pemenang < 0)
            return;

        // Jangan proses kemenangan dua kali.
if (permainanSelesaiOnline.Value)
    return;

// Simpan status pertandingan selesai.
permainanSelesaiOnline.Value = true;

// Hentikan proses giliran dan timer.
sedangProsesOnline.Value = true;
waktuOnline.Value = 0;
sisaWaktuHost = 0f;

        Debug.Log(
            "================================\n" +
            "EDU-POLY MULTIPLAYER SELESAI!\n" +
            "PLAYER " + (pemenang + 1) + " MENANG!\n" +
            "Menguasai satu sisi papan.\n" +
            "================================"
        );

        // Kirim pengumuman ke seluruh pemain.
        UmumkanPemenangOnlineRpc(pemenang);
    }

    // =========================================
    // SINKRONISASI LANDMARK MULTIPLAYER
    // =========================================

    [Rpc(SendTo.Everyone)]
    private void SinkronkanLandmarkOnlineRpc(
        int waypoint,
        int tahapBaru)
    {
        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        if (dataManager == null)
        {
            Debug.LogError(
                "MULTIPLAYER: PropertyDataManager " +
                "tidak ditemukan!"
            );

            return;
        }

        DataLahan lahan =
            dataManager.CariLahan(waypoint);

        if (lahan == null)
        {
            Debug.LogWarning(
                "MULTIPLAYER: Lahan WP " +
                waypoint + " tidak ditemukan!"
            );

            return;
        }

        // Perbarui tahap properti di perangkat ini.
        // Fungsi ini tidak memotong saldo.
        dataManager.TerapkanTahapDariJaringan(
            waypoint,
            tahapBaru
        );

        Debug.Log(
            "MULTIPLAYER: Properti " +
            lahan.namaLahan +
            " berubah menjadi tahap " +
            lahan.tahapProperti
        );
    }

[Rpc(SendTo.Everyone)]
private void UmumkanPemenangOnlineRpc(int pemainMenang)
{
    if (pemainMenang < 0 || pemainMenang >= 4)
        return;

    string judul = "EDU-POLY SELESAI!";
    string pesan;

    if (playerIndexLokal == pemainMenang)
    {
        pesan = "SELAMAT! KAMU MENANG!\n" +
                "Kamu berhasil menguasai satu sisi papan.";
    }
    else
    {
        pesan = "PLAYER " + (pemainMenang + 1) +
                " MEMENANGKAN PERMAINAN!\n" +
                "Satu sisi papan berhasil dikuasai.";
    }

    Debug.Log(
        "MULTIPLAYER EDU-POLY: " + pesan
    );

    // Tampilkan pengumuman melalui panel EDUCARD.
    KartuEdupolyEffectManager effectManager =
        FindAnyObjectByType<KartuEdupolyEffectManager>();

    if (effectManager != null)
    {
        effectManager.TampilkanNotifikasiSistem(
            judul,
            pesan,
            8f
        );
    }
    else
    {
        Debug.LogWarning(
            "KartuEdupolyEffectManager tidak ditemukan."
        );
    }

    // Munculkan tombol Kembali ke Menu.
    if (tombolKembaliMenu != null)
    {
        tombolKembaliMenu.SetActive(true);
    }
    }
    
    // Dipanggil ketika pemain menekan tombol
    // Kembali ke Menu setelah pertandingan selesai.
    
    private bool sedangKembaliKeMenu = false;

    public async void KembaliKeMenuSetelahMenang()
    {
        if (sedangKembaliKeMenu)
            return;

        if (!permainanSelesaiOnline.Value)
            return;

        sedangKembaliKeMenu = true;

        try
        {
            var sesi = MultiplayerManager.sesiAktif;

            if (sesi != null)
            {
                if (sesi.IsHost)
                    await sesi.AsHost().DeleteAsync();
                else
                    await sesi.LeaveAsync();

                MultiplayerManager.sesiAktif = null;
            }
        }
        catch (System.Exception error)
        {
            Debug.LogError(
                "Gagal menutup sesi Multiplayer: " +
                error.Message
            );
        }

        // Putuskan koneksi Netcode.
        var network = Unity.Netcode.NetworkManager.Singleton;

        if (network != null && network.IsListening)
            network.Shutdown();

        MainMenuManager.modeMultiplayer = false;

        // Kembali ke menu utama.
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            "MainMenu"
        );
    }


    [Rpc(SendTo.Everyone)]
    private void SinkronkanStatusPenjaraOnlineRpc(
        int pemain,
        bool dipenjara)
    {
        TurnManager turnManager =
            FindAnyObjectByType<TurnManager>();

        if (turnManager == null ||
            turnManager.daftarPion == null ||
            pemain < 0 ||
            pemain >= turnManager.daftarPion.Length)
            return;

        PionMovement pion =
            turnManager.daftarPion[pemain];

        if (pion == null)
            return;

        pion.sedangDipenjara = dipenjara;

        Debug.Log(
            "MULTIPLAYER: Status penjara Player " +
            (pemain + 1) + " = " + dipenjara
        );
    }
    [Rpc(SendTo.Everyone)]
private void SinkronkanPosisiPenjaraOnlineRpc(int pemain)
{
    TurnManager turnManager =
        FindAnyObjectByType<TurnManager>();

    if (turnManager == null ||
        turnManager.daftarPion == null ||
        pemain < 0 ||
        pemain >= turnManager.daftarPion.Length)
        return;

    PionMovement pion =
        turnManager.daftarPion[pemain];

    if (pion == null)
        return;

    if (pion.waypoints == null ||
        pion.waypoints.Length <= 9 ||
        pion.SedangBergerak)
    {
        Debug.LogWarning(
            "MULTIPLAYER: Posisi penjara gagal disinkronkan."
        );
        return;
    }

    // Paksa semua perangkat menempatkan pion di WP 9.
    pion.posisiSekarang = 9;
    pion.sedangDipenjara = true;

    // Perbarui posisi visual pion.
    pion.UpdatePosisiDiam();

    Debug.Log(
        "MULTIPLAYER: Posisi Player " +
        (pemain + 1) +
        " berhasil disinkronkan ke WP 9."
    );
}
}
