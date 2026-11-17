
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class RiwayatPemainTeks : MonoBehaviour
{
    public TMP_Text teksRiwayat;
    public int maksimalRiwayat = 30;

    private readonly List<string> daftarRiwayat = new();
    private PlayerMoneyManager uang;

    private void Start()
    {
        if (teksRiwayat == null) return;

        teksRiwayat.text = "";

        uang = FindAnyObjectByType<PlayerMoneyManager>();

        if (uang != null)
            uang.OnTransaksi += TambahkanRiwayat;
    }

    public void TambahkanRiwayat(
        int idPemain, string jenis, string keterangan,
        int nominal, bool pemasukan)
    {
        string tanda = pemasukan ? "+" : "-";

        string catatan =
            $"Pemain {idPemain} | {jenis}: {keterangan} ({tanda}Rp{nominal:N0})";

        daftarRiwayat.Insert(0, catatan);

        if (daftarRiwayat.Count > maksimalRiwayat)
            daftarRiwayat.RemoveAt(daftarRiwayat.Count - 1);

        StringBuilder hasil = new StringBuilder();

        foreach (string item in daftarRiwayat)
            hasil.AppendLine("• " + item);

        teksRiwayat.text = hasil.ToString();
    }

    private void OnDestroy()
    {
        if (uang != null)
            uang.OnTransaksi -= TambahkanRiwayat;
    }
}
