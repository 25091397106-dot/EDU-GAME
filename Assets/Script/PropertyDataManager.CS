
using System;
using UnityEngine;

[Serializable]
public class DataLahan
{
    public int waypoint;
    public string namaLahan;
    public int harga;
    public Color warnaKomplek;

    // -1 = Belum ada pemilik
    public int pemilik = -1;

    // 0 = Kosong
    // 1 = Tiga Bangunan
    // 2 = Landmark
    public int tahapProperti = 0;
}

public class PropertyDataManager : MonoBehaviour
{
    [Header("Data 24 Lahan Edupoly")]
    public DataLahan[] daftarLahan;

    [Header("Biaya Upgrade Landmark")]
    [Range(0.1f, 2f)]
    public float pengaliBiayaUpgrade = 0.5f;

    private void Awake()
    {
        daftarLahan = new DataLahan[]
        {
            Buat(1, "Seni Rupa", 120000, "#DCA0E5"),
            Buat(2, "Musik", 150000, "#DCA0E5"),
            Buat(4, "Tari", 180000, "#DCA0E5"),

            Buat(6, "Taman Pancasila", 160000, "#D73532"),
            Buat(7, "Jejak Konstitusi", 175000, "#D73532"),
            Buat(8, "Ruang Etika", 200000, "#D73532"),

            Buat(10, "Puisi", 180000, "#824993"),
            Buat(12, "Cerpen", 195000, "#824993"),
            Buat(13, "Ejaan", 210000, "#824993"),

            Buat(14, "Word Gallery", 230000, "#68BCE5"),
            Buat(16, "Talk Avenue", 250000, "#68BCE5"),
            Buat(17, "Sentence Attention", 265000, "#68BCE5"),

            Buat(19, "Pasar", 270000, "#CCE7A8"),
            Buat(21, "Uang", 290000, "#CCE7A8"),
            Buat(22, "Kegiatan Ekonomi", 300000, "#CCE7A8"),

            Buat(23, "Sel", 320000, "#168752"),
            Buat(25, "Makhluk Hidup", 340000, "#168752"),
            Buat(26, "Ekosistem", 355000, "#168752"),

            Buat(28, "Aljabar", 370000, "#2657A5"),
            Buat(29, "Bangun Datar", 390000, "#2657A5"),
            Buat(31, "Pecahan", 430000, "#2657A5"),

            Buat(32, "Gerak", 450000, "#8C1B19"),
            Buat(34, "Gaya", 480000, "#8C1B19"),
            Buat(35, "Energi", 500000, "#8C1B19")
        };
    }

    private DataLahan Buat(
        int waypoint,
        string nama,
        int harga,
        string hex)
    {
        Color warna = Color.white;

        ColorUtility.TryParseHtmlString(
            hex,
            out warna
        );

        return new DataLahan
        {
            waypoint = waypoint,
            namaLahan = nama,
            harga = harga,
            warnaKomplek = warna,
            pemilik = -1,
            tahapProperti = 0
        };
    }

    public DataLahan CariLahan(int waypoint)
    {
        if (daftarLahan == null)
            return null;

        foreach (DataLahan lahan in daftarLahan)
        {
            if (lahan != null &&
                lahan.waypoint == waypoint)
            {
                return lahan;
            }
        }

        return null;
    }

    // ====================================
    // PEMBELIAN LAHAN - LEVEL 1
    // ====================================
    public bool BeliLahan(
        DataLahan lahan,
        int idPemain,
        PlayerMoneyManager uang)
    {
        if (lahan == null ||
            uang == null ||
            idPemain < 0 ||
            lahan.pemilik != -1)
        {
            return false;
        }

        if (!uang.KurangiSaldo(
            idPemain,
            lahan.harga))
        {
            Debug.Log(
                "Saldo tidak cukup membeli " +
                lahan.namaLahan
            );

            return false;
        }

        lahan.pemilik = idPemain;
        lahan.tahapProperti = 1;

        Debug.Log(
            "Lahan dibeli: " +
            lahan.namaLahan +
            " | 3 bangunan aktif."
        );

        return true;
    }

    // ====================================
    // HITUNG BIAYA LANDMARK
    // ====================================
    public int HitungBiayaUpgrade(DataLahan lahan)
    {
        if (lahan == null)
            return 0;

        return Mathf.RoundToInt(
            lahan.harga * pengaliBiayaUpgrade
        );
    }

    // ====================================
    // UPGRADE LANDMARK - LEVEL 2
    // Panggil hanya setelah soal benar.
    // ====================================
    public bool UpgradeLandmark(
        DataLahan lahan,
        int idPemain,
        PlayerMoneyManager uang)
    {
        if (lahan == null ||
            uang == null ||
            idPemain < 0)
        {
            return false;
        }

        // Pemain harus pemilik lahan.
        if (lahan.pemilik != idPemain)
        {
            Debug.LogWarning(
                "Upgrade gagal: bukan pemilik lahan."
            );

            return false;
        }

        // Hanya lahan tahap 1 yang bisa upgrade.
        if (lahan.tahapProperti != 1)
        {
            Debug.LogWarning(
                "Upgrade gagal: tahap properti tidak sesuai."
            );

            return false;
        }

        int biaya = HitungBiayaUpgrade(lahan);

        // Pembayaran harus berhasil dulu.
        if (!uang.KurangiSaldo(
            idPemain,
            biaya))
        {
            Debug.LogWarning(
                "Saldo tidak cukup untuk upgrade " +
                lahan.namaLahan
            );

            return false;
        }

        // Setelah pembayaran berhasil:
        lahan.tahapProperti = 2;

        Debug.Log(
            "LANDMARK BERHASIL! " +
            lahan.namaLahan +
            " | Biaya Rp" +
            biaya.ToString("N0")
        );

        return true;
    }

    // ====================================
    // SINKRON PEMILIK DARI MULTIPLAYER
    // ====================================
    public void TerapkanPemilikDariJaringan(
        int waypoint,
        int idPemilik)
    {
        DataLahan lahan = CariLahan(waypoint);

        if (lahan == null)
            return;

        lahan.pemilik = idPemilik;

        if (idPemilik == -1)
        {
            lahan.tahapProperti = 0;
        }
        else if (lahan.tahapProperti == 0)
        {
            lahan.tahapProperti = 1;
        }
    }

    // ====================================
    // SINKRON TAHAP LANDMARK MULTIPLAYER
    // Tidak melakukan pembayaran.
    // ====================================
    public void TerapkanTahapDariJaringan(
        int waypoint,
        int tahapBaru)
    {
        DataLahan lahan = CariLahan(waypoint);

        if (lahan == null)
            return;

        if (lahan.pemilik == -1)
            return;

        if (tahapBaru < 1 || tahapBaru > 2)
            return;

        lahan.tahapProperti = tahapBaru;
    }
}
