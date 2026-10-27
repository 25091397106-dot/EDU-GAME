
using System;
using UnityEngine;

[Serializable]
public class DataLahan
{
    public int waypoint;
    public string namaLahan;
    public int harga;
    public Color warnaKomplek;
    public int pemilik = -1; // -1 berarti belum dimiliki
}

public class PropertyDataManager : MonoBehaviour
{
    [Header("Data 24 Lahan Edupoly")]
    public DataLahan[] daftarLahan;

    void Awake()
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

    DataLahan Buat(int waypoint, string nama, int harga, string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color warna);

        return new DataLahan
        {
            waypoint = waypoint,
            namaLahan = nama,
            harga = harga,
            warnaKomplek = warna,
            pemilik = -1
        };
    }

    public DataLahan CariLahan(int waypoint)
    {
        foreach (DataLahan lahan in daftarLahan)
        {
            if (lahan.waypoint == waypoint)
                return lahan;
        }

        return null;
    }
}
