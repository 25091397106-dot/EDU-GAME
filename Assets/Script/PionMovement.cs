
using System.Collections;
using UnityEngine;

public class PionMovement : MonoBehaviour
{
    [Header("Pengaturan Waypoint")]
    public Transform[] waypoints;
    public Transform wadahWaypoints;

    [Header("Pengaturan Pergerakan")]
    public int posisiSekarang = 0;
    public float kecepatanGerak = 3f;
    public float tinggiPion = 0.1f;

    [Header("Referensi")]
    public CameraFollow cameraFollow;
    public TurnManager turnManager;

    private bool sedangBergerak = false;

    public bool SedangBergerak => sedangBergerak;
    [Header("Sistem Putaran dan START")]
    public int putaranSelesai = 0;
    public int bonusStart = 150000;

    [Header("Status Penjara")]
    public bool sedangDipenjara = false;

    public bool BolehBeliLahan => putaranSelesai >= 1;

    void Start()
    {
        // Ambil seluruh waypoint secara otomatis
        if (wadahWaypoints != null)
        {
            int jumlahWaypoint = wadahWaypoints.childCount;
            waypoints = new Transform[jumlahWaypoint];

            for (int i = 0; i < jumlahWaypoint; i++)
            {
                waypoints[i] = wadahWaypoints.GetChild(i);
            }
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError(
                gameObject.name + ": Waypoints belum diisi!"
            );
            return;
        }

        posisiSekarang = Mathf.Clamp(
            posisiSekarang, 0, waypoints.Length - 1
        );

        UpdatePosisiDiam();
    }

    public void Bergerak(int jumlahLangkah)
    {
        if (sedangBergerak || jumlahLangkah <= 0)
            return;

        if (waypoints == null || waypoints.Length == 0)
            return;

        // Langsung tandai bergerak agar TurnManager
        // tidak salah mengira gerakan sudah selesai.
        sedangBergerak = true;

        StartCoroutine(AnimasiBergerak(jumlahLangkah));
    }

    IEnumerator AnimasiBergerak(int jumlahLangkah)
    {
        // Kamera mengikuti pion yang berjalan
        if (cameraFollow != null)
        {
            cameraFollow.IkutiPion(transform);
        }

        for (int i = 0; i < jumlahLangkah; i++)
        {
            int posisiBerikutnya =
                (posisiSekarang + 1) % waypoints.Length;

            Vector3 tujuan =
                waypoints[posisiBerikutnya].position +
                Vector3.up * tinggiPion;

            while (Vector3.Distance(
                transform.position, tujuan
            ) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    tujuan,
                    kecepatanGerak * Time.deltaTime
                );

                yield return null;
            }

            transform.position = tujuan;
            posisiSekarang = posisiBerikutnya;

            // TAMBAHAN: Mendeteksi pion mencapai START
            if (posisiSekarang == 0)
            {
                putaranSelesai++;

                Debug.Log(
                    gameObject.name +
                    " menyelesaikan " + putaranSelesai +
                    " putaran!"
                );

                // Bonus hanya jika masih melanjutkan langkah
                // melewati START, bukan berhenti tepat di START.
                if (i < jumlahLangkah - 1)
                {
                    BerikanBonusStart();
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        sedangBergerak = false;

        Debug.Log(
            gameObject.name +
            " berhenti di waypoint " +
            posisiSekarang
        );

        // Rapikan semua pion setelah gerakan selesai
        if (turnManager != null)
        {
            turnManager.RapikanSemuaPion();
        }
        else
        {
            UpdatePosisiDiam();
        }

        // Kembalikan kamera ke tampilan papan
        if (cameraFollow != null)
        {
            cameraFollow.BerhentiMengikuti();
        }
    }

    // Menghitung posisi saat beberapa pion
    // berada pada petak yang sama
    Vector3 AmbilOffsetDinamis()
    {
        if (turnManager == null ||
            turnManager.daftarPion == null)
        {
            return Vector3.zero;
        }

        PionMovement[] pionSatuKotak =
            new PionMovement[4];

        int jumlah = 0;

        for (int i = 0;
             i < turnManager.daftarPion.Length;
             i++)
        {
            PionMovement pion =
                turnManager.daftarPion[i];

            if (pion != null &&
                pion.gameObject.activeInHierarchy &&
                pion.posisiSekarang == posisiSekarang)
            {
                if (jumlah < pionSatuKotak.Length)
                {
                    pionSatuKotak[jumlah] = pion;
                    jumlah++;
                }
            }
        }

        // Sendirian: tepat di tengah petak
        if (jumlah <= 1)
            return Vector3.zero;

        int urutan = 0;

        for (int i = 0; i < jumlah; i++)
        {
            if (pionSatuKotak[i] == this)
            {
                urutan = i;
                break;
            }
        }

        Vector3[] offsets =
        {
            new Vector3(-0.15f, 0,  0.15f),
            new Vector3( 0.15f, 0,  0.15f),
            new Vector3(-0.15f, 0, -0.15f),
            new Vector3( 0.15f, 0, -0.15f)
        };

        return offsets[Mathf.Clamp(
            urutan, 0, offsets.Length - 1
        )];
    }

    // Memposisikan ulang pion yang sedang diam
    public void UpdatePosisiDiam()
    {
        if (sedangBergerak)
            return;

        if (waypoints == null ||
            waypoints.Length == 0)
            return;

        if (posisiSekarang < 0 ||
            posisiSekarang >= waypoints.Length)
            return;

        transform.position =
            waypoints[posisiSekarang].position +
            Vector3.up * tinggiPion +
            AmbilOffsetDinamis();
    }

    // Masuk penjara setelah tiga kali dadu kembar
    public void MasukPenjara()
    {
        if (sedangBergerak)
        {
            Debug.LogWarning(
                gameObject.name + " masih bergerak!"
            );
            return;
        }

        if (waypoints == null ||
            waypoints.Length <= 9)
        {
            Debug.LogError(
                "Waypoint penjara belum tersedia!"
            );
            return;
        }

        // WP_09_PENJARA
        posisiSekarang = 9;
        sedangDipenjara = true;

        if (turnManager != null)
        {
            turnManager.RapikanSemuaPion();
        }
        else
        {
            UpdatePosisiDiam();
        }

        if (cameraFollow != null)
        {
            cameraFollow.BerhentiMengikuti();
        }

        Debug.Log(
            gameObject.name + " masuk PENJARA!"
        );
    }

    void BerikanBonusStart()
    {
        // Multiplayer: hanya Host yang boleh menghitung uang.
        if (MainMenuManager.modeMultiplayer)
        {
            var network = Unity.Netcode.NetworkManager.Singleton;

            if (network == null || !network.IsServer)
                return;
        }

        if (turnManager == null ||
            turnManager.daftarPion == null)
            return;

        // Cari nomor pemain berdasarkan pion yang bergerak.
        int idPemain = -1;

        for (int i = 0; i < turnManager.daftarPion.Length; i++)
        {
            if (turnManager.daftarPion[i] == this)
            {
                idPemain = i;
                break;
            }
        }

        if (idPemain < 0)
            return;

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang == null)
        {
            Debug.LogWarning(
                "PlayerMoneyManager belum ditemukan!"
            );
            return;
        }

        uang.TambahSaldo(idPemain, bonusStart);

        Debug.Log(
            "Player " + (idPemain + 1) +
            " melewati START! Bonus Rp" +
            bonusStart.ToString("N0")
        );
    }

}