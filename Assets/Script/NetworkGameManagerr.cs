
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
    // Menandai proses pembelian lahan multiplayer
    private bool sedangPembelianOnline = false;
    private int pemainPembeliOnline = -1;
    private int waypointPembelianOnline = -1;

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
        if (!masukPenjara)
        {
            PionMovement pion = turnManager.daftarPion[pemain];

            PropertyDataManager dataManager =
                FindAnyObjectByType<PropertyDataManager>();

            if (dataManager != null && pion.BolehBeliLahan)
            {
                DataLahan lahan =
                    dataManager.CariLahan(pion.posisiSekarang);

                if (lahan != null &&
                    lahan.pemilik == -1 &&
                    lahan.harga > 0)
                {
                    sedangPembelianOnline = true;
                    pemainPembeliOnline = pemain;
                    waypointPembelianOnline = pion.posisiSekarang;

                    TampilkanPembelianOnlineRpc(
                        pemain,
                        pion.posisiSekarang
                    );

                    float waktuTungguPembelian = 60f;

                    yield return new WaitUntil(() =>
                    {
                        waktuTungguPembelian -= Time.deltaTime;

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
            }
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
    private void TutupPembelianOnlineRpc()
    {
        PropertyPurchaseUI purchaseUI =
            FindAnyObjectByType<PropertyPurchaseUI>();

        if (purchaseUI == null)
            return;

        // Hentikan timer soal ketika pembelian online selesai
        SoalManager soalManager =
            FindAnyObjectByType<SoalManager>();

        if (soalManager != null)
        {
            soalManager.BatalkanSoal();
        }

        CardDisplayManager kartu =
            FindAnyObjectByType<CardDisplayManager>();

        if (kartu != null)
        {
            kartu.SembunyikanKartu();
        }

        purchaseUI.TutupPanel();
        purchaseUI.saatKlikBeli = null;
        purchaseUI.saatKlikLewati = null;
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

        lahan.pemilik = pemilik;

        Debug.Log(
            "MULTIPLAYER: Lahan " + lahan.namaLahan +
            " dimiliki Player " + (pemilik + 1)
        );
    }
    [Rpc(SendTo.Everyone)]
    private void TampilkanPembelianOnlineRpc(int pemain, int waypoint)
    {
        // Hanya pemain yang sedang giliran melihat panel
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

        // Bersihkan pilihan sebelumnya agar tidak terpanggil dua kali
        purchaseUI.saatKlikBeli = null;
        purchaseUI.saatKlikLewati = null;

        // Simpan pilihan pemain
        purchaseUI.saatKlikBeli = () =>
        {
            Debug.Log("MULTIPLAYER: Pemain memilih BELI " + lahan.namaLahan);
        };
        CardDisplayManager kartu =
            FindAnyObjectByType<CardDisplayManager>();

        if (kartu != null)
        {
            SoalManager soalManager =
                FindAnyObjectByType<SoalManager>();

            if (soalManager == null)
            {
                Debug.LogError("SoalManager tidak ditemukan!");
                return;
            }

            System.Action<bool> hasilJawaban = null;

            hasilJawaban = (benar) =>
            {
                soalManager.OnJawabanSelesai -= hasilJawaban;

                // Jangan kirim hasil jika proses pembelian sudah berakhir
                if (!sedangProsesOnline.Value)
                    return;

                KirimHasilSoalOnlineRpc(
                    pemain,
                    waypoint,
                    benar
                );
            };

            soalManager.OnJawabanSelesai += hasilJawaban;

            bool berhasil =
                kartu.TampilkanKartuSesuaiPetak(lahan.namaLahan);

            if (!berhasil)
            {
                soalManager.OnJawabanSelesai -= hasilJawaban;
                Debug.LogWarning("Kartu soal gagal ditampilkan!");
            }
        }
        else
        {
            Debug.LogError("CardDisplayManager tidak ditemukan!");
        }

        purchaseUI.saatKlikLewati = () =>
        {
            Debug.Log("MULTIPLAYER: Pemain memilih LEWATI " + lahan.namaLahan);
        };

        KirimLewatiPembelianOnlineRpc(pemain, waypoint);

        purchaseUI.TampilkanPembelian(
            lahan.namaLahan,
            lahan.harga,
            lahan.warnaKomplek
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
