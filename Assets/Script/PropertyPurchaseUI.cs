
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PropertyPurchaseUI : MonoBehaviour
{
    [Header("Panel Pembelian")]
    public GameObject panelPembelian;

    [Header("Tampilan Lahan")]
    public TMP_Text namaLahan;
    public TMP_Text hargaLahan;
    public Image backgroundJudul;

    [Header("Tombol")]
    public Button buttonBeli;
    public Button buttonLewati;

    // Data lahan yang sedang ditampilkan
    public string namaLahanAktif;
    public int hargaLahanAktif;

    // Nanti dipakai sistem soal
    public Action saatKlikBeli;
    public Action saatKlikLewati;

    void Start()
    {
        // Hubungkan tombol secara otomatis
        if (buttonBeli != null)
            buttonBeli.onClick.AddListener(KlikBeli);

        if (buttonLewati != null)
            buttonLewati.onClick.AddListener(KlikLewati);

        // Panel tidak muncul saat awal game
        TutupPanel();
    }

    public void TampilkanPembelian(
        string nama,
        int harga,
        Color warnaKomplek)
    {
        namaLahanAktif = nama;
        hargaLahanAktif = harga;

        if (namaLahan != null)
            namaLahan.text = nama.ToUpper();

        if (hargaLahan != null)
            hargaLahan.text = "Rp. " + harga.ToString("N0");

        if (backgroundJudul != null)
            backgroundJudul.color = warnaKomplek;

        if (panelPembelian != null)
            panelPembelian.SetActive(true);
    }

    public void KlikBeli()
    {
        Debug.Log(
            "Pemain memilih BELI: " + namaLahanAktif
        );

        TutupPanel();

        // Nanti memunculkan soal pada kartu 3D
        saatKlikBeli?.Invoke();
    }

    public void KlikLewati()
    {
        Debug.Log(
            "Pemain melewati lahan: " + namaLahanAktif
        );

        TutupPanel();

        saatKlikLewati?.Invoke();
    }

    public void TutupPanel()
    {
        if (panelPembelian != null)
            panelPembelian.SetActive(false);
    }
    [ContextMenu("Tes Panel Seni Rupa")]
    public void TesPanelSeniRupa()
    {
        PropertyDataManager dataManager =
            FindAnyObjectByType<PropertyDataManager>();

        if (dataManager == null)
        {
            Debug.LogWarning("PropertyDataManager tidak ditemukan!");
            return;
        }

        DataLahan lahan = dataManager.CariLahan(1);

        if (lahan == null)
            return;

        TampilkanPembelian(
            lahan.namaLahan,
            lahan.harga,
            lahan.warnaKomplek
        );
    }
}
