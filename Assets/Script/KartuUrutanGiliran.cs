
using System.Collections.Generic;
using UnityEngine;

public static class KartuUrutanGiliran
{
    
    // Nilai kartu untuk setiap pemain.
    // Indeks 0 = Player 1, indeks 1 = Player 2, dst.
    public static int[] BuatAngkaKartu(int[] urutan)
    {
        if (urutan == null)
            return new int[0];

        int[] angkaKartu = new int[urutan.Length];

        for (int posisi = 0; posisi < urutan.Length; posisi++)
        {
            int pemain = urutan[posisi];

            // Kartu tertinggi mendapat giliran pertama.
            angkaKartu[pemain] = urutan.Length - posisi;
        }

        return angkaKartu;
    }

    public static int[] AcakUrutan(int jumlahPemain)
    {
        jumlahPemain = Mathf.Clamp(jumlahPemain, 2, 4);

        List<int> urutan = new List<int>();

        for (int i = 0; i < jumlahPemain; i++)
            urutan.Add(i);

        // Acak kartu menggunakan algoritma Fisher-Yates.
        for (int i = urutan.Count - 1; i > 0; i--)
        {
            int acak = Random.Range(0, i + 1);

            int sementara = urutan[i];
            urutan[i] = urutan[acak];
            urutan[acak] = sementara;
        }

        return urutan.ToArray();
    }
}
