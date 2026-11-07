
using UnityEngine;

public static class SideOwnershipWinChecker
{
    // Enam properti pada setiap sisi papan.
    // Petak khusus tidak dihitung.
    private static readonly int[][] sisiPapan =
    {
        // Bawah: Seni, Pancasila, Etika
        new int[] { 1, 2, 4, 6, 7, 8 },

        // Kanan: Bahasa dan Sastra
        new int[] { 10, 12, 13, 14, 16, 17 },

        // Atas: Ekonomi dan IPA
        new int[] { 19, 21, 22, 23, 25, 26 },

        // Kiri: Matematika dan Fisika
        new int[] { 28, 29, 31, 32, 34, 35 }
    };

    // Mengembalikan ID pemenang (0–3),
    // atau -1 jika belum ada pemenang.
    public static int CariPemenangSatuSisi(
        PropertyDataManager dataManager)
    {
        if (dataManager == null)
            return -1;

        foreach (int[] sisi in sisiPapan)
        {
            int calonPemenang = -1;
            bool semuaDimiliki = true;

            foreach (int waypoint in sisi)
            {
                DataLahan lahan =
                    dataManager.CariLahan(waypoint);

                if (lahan == null || lahan.pemilik < 0)
                {
                    semuaDimiliki = false;
                    break;
                }

                if (calonPemenang == -1)
                {
                    calonPemenang = lahan.pemilik;
                }
                else if (lahan.pemilik != calonPemenang)
                {
                    semuaDimiliki = false;
                    break;
                }
            }

            if (semuaDimiliki && calonPemenang >= 0)
            {
                Debug.Log(
                    "PLAYER " + (calonPemenang + 1) +
                    " menguasai seluruh properti " +
                    "pada satu sisi papan!"
                );

                return calonPemenang;
            }
        }

        return -1;
    }
}
