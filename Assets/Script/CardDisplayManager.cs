using System.Collections.Generic;
using UnityEngine;

public class CardDisplayManager : MonoBehaviour
{
    [Header("Referensi")]
    public Camera kameraGame;
    public GameObject kartu3D;
    [Header("Daftar Kartu Berdasarkan Petak")]
    public List<GameObject> daftarKartu = new List<GameObject>();

    [Header("Tampilan Kartu")]
    public float jarakKamera = 4f;
    public float ukuranKartu = 2f;
    [Header("Posisi Kartu di Layar")]

    private Vector3 posisiAsli;
    private Quaternion rotasiAsli;
    private Vector3 skalaAsli;
    [Header("Offset Tampilan")]
    public Vector3 offsetTampilan = new Vector3(-0.8f, -0.15f, 0f);

    void Start()
    {
        if (kameraGame == null)
            kameraGame = Camera.main;

        if (kartu3D == null)
            return;

        posisiAsli = kartu3D.transform.position;
        rotasiAsli = kartu3D.transform.rotation;
        skalaAsli = kartu3D.transform.localScale;

        kartu3D.SetActive(false);
    }
    public bool TampilkanKartuSesuaiPetak(string namaPetak)
    {
        if (string.IsNullOrWhiteSpace(namaPetak))
            return false;

        foreach (GameObject kartu in daftarKartu)
        {
            if (kartu == null)
                continue;

            KartuSoal dataKartu = kartu.GetComponent<KartuSoal>();

            if (dataKartu == null)
                continue;

            if (string.Equals(
                dataKartu.namaPetak.Trim(),
                namaPetak.Trim(),
                System.StringComparison.OrdinalIgnoreCase))
            {
                // Sembunyikan kartu sebelumnya
                if (kartu3D != null)
                    SembunyikanKartu();

                // Pilih kartu yang sesuai
                kartu3D = kartu;

                // Simpan transform asli kartu baru
                posisiAsli = kartu.transform.position;
                rotasiAsli = kartu.transform.rotation;
                skalaAsli = kartu.transform.localScale;

                // Tampilkan menggunakan fungsi lama
                TampilkanKartu();

                // Isi soal sesuai petaknya
                dataKartu.TampilkanSoal();

                Debug.Log("Kartu otomatis muncul: " + namaPetak);

                return true;
            }
        }

        Debug.LogWarning("Kartu tidak ditemukan untuk petak: " + namaPetak);
        return false;
    }
    public void TampilkanKartu()
    {
        if (kameraGame == null || kartu3D == null)
            return;

        Transform cam = kameraGame.transform;

        kartu3D.transform.position =
            cam.position + cam.forward * jarakKamera;
        // TAMBAHAN: Geser kartu sedikit ke kiri dan ke bawah
        kartu3D.transform.position +=
            cam.right * offsetTampilan.x +
            cam.up * offsetTampilan.y +
            cam.forward * offsetTampilan.z;
        // Menghadap ke kamera.
        kartu3D.transform.rotation =
            Quaternion.LookRotation(-cam.forward, cam.up);

        // TAMBAHAN: Balik kartu agar sisi depan menghadap kamera
        kartu3D.transform.Rotate(0f, 180f, 0f, Space.Self);

        kartu3D.transform.localScale =
            Vector3.one * ukuranKartu;
        // Lebar kartu dibuat sedikit lebih besar
        kartu3D.transform.localScale = new Vector3(
            ukuranKartu * 1.4f,
            ukuranKartu,
            ukuranKartu
        );
        kartu3D.SetActive(true);
    }

    public void SembunyikanKartu()
    {
        if (kartu3D == null)
            return;

        kartu3D.SetActive(false);

        kartu3D.transform.position = posisiAsli;
        kartu3D.transform.rotation = rotasiAsli;
        kartu3D.transform.localScale = skalaAsli;
    }

    [ContextMenu("Tes Tampilkan Kartu")]
    public void TesTampilkanKartu()
    {
        TampilkanKartu();
    }

    [ContextMenu("Tes Sembunyikan Kartu")]
    public void TesSembunyikanKartu()
    {
        SembunyikanKartu();
    }
}
