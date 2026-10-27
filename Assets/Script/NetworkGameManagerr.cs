
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class NetworkGameManager : NetworkBehaviour
{
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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            AturIdentitasPemain();
            giliranOnline.Value = 0;
            waktuOnline.Value = 15;
            sedangProsesOnline.Value = false;
        }

        PerbaruiIdentitasLokal();

        Debug.Log("NetworkGameManager aktif.");
    }

    // Host mencatat seluruh pemain yang terhubung
    private void AturIdentitasPemain()
    {
        List<ulong> ids = new List<ulong>(
            NetworkManager.ConnectedClientsIds
        );

        ids.Remove(NetworkManager.ServerClientId);
        ids.Sort();

        ids.Insert(0, NetworkManager.ServerClientId);

        jumlahPemainOnline = Mathf.Min(ids.Count, 4);

        slot1.Value = ids.Count > 0
            ? ids[0] : ulong.MaxValue;

        slot2.Value = ids.Count > 1
            ? ids[1] : ulong.MaxValue;

        slot3.Value = ids.Count > 2
            ? ids[2] : ulong.MaxValue;

        slot4.Value = ids.Count > 3
            ? ids[3] : ulong.MaxValue;
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

        // KODE BARU: Tampilkan timer jaringan di semua perangkat
        TurnManager turnManagerTimer =
            FindAnyObjectByType<TurnManager>();

        if (turnManagerTimer != null &&
     turnManagerTimer.textTimer != null)
        {
            // Timer hanya muncul di pemain yang sedang giliran
            if (playerIndexLokal == giliranOnline.Value)
            {
                turnManagerTimer.textTimer.text =
                    waktuOnline.Value.ToString();
            }
            else
            {
                // Sembunyikan timer untuk pemain yang menunggu
                turnManagerTimer.textTimer.text = "";
            }
        }

        // Perbarui identitas berdasarkan data Host
        PerbaruiIdentitasLokal();

        // Hanya Host yang menghitung timer
        if (!IsServer)
            return;

        if (sedangProsesOnline.Value)
            return;

        sisaWaktuHost -= Time.deltaTime;
        sisaWaktuHost = Mathf.Max(0f, sisaWaktuHost);

        int detik = Mathf.CeilToInt(sisaWaktuHost);

        if (waktuOnline.Value != detik)
        {
            waktuOnline.Value = detik;
        }

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
    public void GiliranOnlineBerikutnya()
    {
        if (!IsServer)
            return;

        int jumlah = Mathf.Max(2, jumlahPemainOnline);

        giliranOnline.Value =
            (giliranOnline.Value + 1) % jumlah;

        ResetTimerOnline();
    }

    public void ResetTimerOnline()
    {
        if (!IsServer)
            return;

        sisaWaktuHost = 15f;
        waktuOnline.Value = 15;
        sedangProsesOnline.Value = false;
    }

    private int jumlahKembarOnline = 0;

    private IEnumerator ProsesLemparanOnline()
    {
        if (!IsServer)
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

        if (pemain < 0 ||
            pemain >= turnManager.daftarPion.Length ||
            turnManager.daftarPion[pemain] == null)
        {
            Debug.LogError("Pion multiplayer tidak ditemukan!");
            ResetTimerOnline();
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

            // Tunggu pion Host selesai bergerak
            yield return new WaitUntil(
                () => !pion.SedangBergerak
            );
        }

        yield return new WaitForSeconds(1f);

        if (kembar && !masukPenjara)
        {
            // Kembar pertama / kedua: pemain yang sama
            Debug.Log("DADU KEMBAR! LEMPAR LAGI.");
            ResetTimerOnline();
        }
        else
        {
            // Tidak kembar / kembar ketiga: ganti pemain
            jumlahKembarOnline = 0;
            GiliranOnlineBerikutnya();
        }
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
}
