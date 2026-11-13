
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
    if (sedangBergerak || jumlahLangkah == 0)
        return;

    if (waypoints == null || waypoints.Length == 0)
        return;

    sedangBergerak = true;
    StartCoroutine(AnimasiBergerak(jumlahLangkah));
}

IEnumerator AnimasiBergerak(int jumlahLangkah)
{
    if (cameraFollow != null)
        cameraFollow.IkutiPion(transform);

    int arah = jumlahLangkah > 0 ? 1 : -1;
    int totalLangkah = Mathf.Abs(jumlahLangkah);

    for (int i = 0; i < totalLangkah; i++)
    {
        int posisiBerikutnya =
            (posisiSekarang + arah + waypoints.Length)
            % waypoints.Length;

        Vector3 tujuan =
            waypoints[posisiBerikutnya].position +
            Vector3.up * tinggiPion;

        while (Vector3.Distance(transform.position, tujuan) > 0.01f)
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

        // Bonus START hanya ketika bergerak maju melewati START.
        if (arah > 0 && posisiSekarang == 0)
        {
            putaranSelesai++;

            Debug.Log(
                gameObject.name +
                " menyelesaikan " + putaranSelesai + " putaran!"
            );

            if (i < totalLangkah - 1)
                BerikanBonusStart();
        }

        // Jika tepat mendarat di WP 27, langsung masuk penjara WP 9.
        if (arah > 0 && posisiSekarang == 27)
        {
            if (waypoints.Length <= 27 || waypoints.Length <= 9)
            {
                Debug.LogError(
                    "Waypoint 27 atau waypoint 9 belum tersedia!"
                );
            }
            else
            {
                posisiSekarang = 9;
                sedangDipenjara = true;

                transform.position =
                    waypoints[9].position +
                    Vector3.up * tinggiPion;

                Debug.Log(
                    gameObject.name +
                    " mendarat di WP 27 dan langsung masuk PENJARA WP 9!"
                );

                // Hentikan sisa langkah dadu.
                break;
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

    if (turnManager != null)
        turnManager.RapikanSemuaPion();
    else
        UpdatePosisiDiam();

    if (cameraFollow != null)
        cameraFollow.BerhentiMengikuti();
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

    // Pastikan WP 27 dan WP 9 tersedia.
    if (waypoints == null ||
        waypoints.Length <= 27)
    {
        Debug.LogError(
            "Waypoint 27 atau waypoint 9 belum tersedia!"
        );
        return;
    }

    // ==================================
    // LANGKAH 1: MASUK KE WP 27
    // ==================================

    posisiSekarang = 27;

    transform.position =
        waypoints[27].position +
        Vector3.up * tinggiPion +
        AmbilOffsetDinamis();

    Debug.Log(
        gameObject.name +
        " diarahkan ke WP 27."
    );

    // ==================================
    // LANGKAH 2: LANGSUNG KE WP 9
    // ==================================

    posisiSekarang = 9;
    sedangDipenjara = true;

    transform.position =
        waypoints[9].position +
        Vector3.up * tinggiPion +
        AmbilOffsetDinamis();

    // Rapikan posisi pion di penjara.
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
        gameObject.name +
        " masuk PENJARA melalui WP 27 dan berakhir di WP 9!"
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